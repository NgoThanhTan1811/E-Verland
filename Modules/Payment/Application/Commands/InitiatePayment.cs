using System.Diagnostics;
using System.Data;
using Amazon.XRay.Recorder.Core;
using Infra.AWS.CloudWatch;
using Infra.AWS.EventBridge;
using Infra.AWS.SNS;
using Infra.AWS.SQS;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Modules.Order.Application.Contracts;
using Modules.Payment.Application.Contracts;
using Modules.Payment.Application.Helpers;
using Modules.Payment.Domain;
using Modules.Payment.Infrastructure.Persistence;
using Modules.Product.Application.Contracts;

namespace Modules.Payment.Application.Commands;

public sealed record OrderItemDto(Guid SkuId, int Quantity);

public sealed record InitiatePaymentCommand(
    Guid OrderId,
    Guid UserId,
    decimal Amount,
    PaymentMethod Method,
    List<OrderItemDto> Items
) : IRequest<InitiatePaymentResponseDto>;

public sealed record InitiatePaymentResponseDto(
    Guid Id,
    string Code,
    PaymentStatus Status,
    string? PaymentUrl
);

public sealed class InitiatePaymentHandler(
    IPaymentRepository repo,
    PaymentDbContext paymentDbContext,
    IProductReservationService reservationService,
    ISePayClient sePayClient,
    ICloudWatchService cloudWatch,
    ILogger<InitiatePaymentHandler> logger,
    IOrderPaymentSyncService orderPaymentSyncService,
    IConfiguration? configuration = null,
    ISQSService? sqsService = null,
    ISNSService? snsService = null,
    IEventBridgeService? eventBridgeService = null
) : IRequestHandler<InitiatePaymentCommand, InitiatePaymentResponseDto>
{
    private readonly IConfiguration? _configuration = configuration;
    private readonly ISQSService? _sqsService = sqsService;
    private readonly ISNSService? _snsService = snsService;
    private readonly IEventBridgeService? _eventBridgeService = eventBridgeService;
    private readonly PaymentDbContext _paymentDbContext = paymentDbContext;

    public async Task<InitiatePaymentResponseDto> Handle(InitiatePaymentCommand request, CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();

        // Idempotency check
        var existing = await repo.GetByOrderIdAsync(request.OrderId, ct);
        if (existing != null)
            throw new InvalidOperationException("Payment already exists for this order.");

        var payment = new Domain.Payment
        {
            Code = PaymentCodeHelper.Generate(),
            OrderId = request.OrderId,
            UserId = request.UserId,
            Amount = request.Amount,
            Method = request.Method,
            Status = PaymentStatus.Pending
        };

        var stockReserved = false;

        // Wrap DB operations in X-Ray subsegment
        AWSXRayRecorder.Instance.BeginSubsegment("Payment.DB");
        try
        {
            // Reserve stock before persisting
            await reservationService.ReserveStockAsync(
                request.OrderId,
                payment.Id,
                request.Items.Select(i => (i.SkuId, i.Quantity)),
                ct);
            stockReserved = true;

            await using var tx = await _paymentDbContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
            try
            {
                await repo.CreateAsync(payment, ct);

                // If OnlineBanking, create SePay payment link
                if (request.Method == PaymentMethod.OnlineBanking)
                {
                    var paymentUrl = await sePayClient.CreatePaymentLinkAsync(
                        payment.Code,
                        payment.Amount,
                        $"Thanh toan don hang {payment.OrderId}",
                        ct);
                    payment.PaymentUrl = paymentUrl;
                }

                await _paymentDbContext.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
                await orderPaymentSyncService.SyncPaymentAsync(
                    payment.OrderId,
                    payment.Id,
                    payment.Status.ToString(),
                    ct);
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync(ct);
                throw new InvalidOperationException("Payment persistence failed.", ex);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Exception? compensationError = null;
            try
            {
                if (stockReserved)
                {
                    await reservationService.ReleaseReservationAsync(payment.Id, ct);
                }

                await orderPaymentSyncService.SyncPaymentAsync(
                    payment.OrderId,
                    payment.Id,
                    PaymentStatus.Pending.ToString(),
                    ct);
            }
            catch (Exception compEx)
            {
                compensationError = compEx;
            }

            if (compensationError is not null)
            {
                throw new AggregateException(
                    "Payment initiation failed and compensation also failed.",
                    ex,
                    compensationError);
            }

            throw new InvalidOperationException("Payment initiation failed and compensation has been applied.", ex);
        }
        finally
        {
            AWSXRayRecorder.Instance.EndSubsegment();
        }

        stopwatch.Stop();

        // Emit CloudWatch metrics on success
        await cloudWatch.PutMetricAsync("payment.initiated", 1, "Count", ct: ct);
        await cloudWatch.PutMetricAsync("payment.latency_ms", stopwatch.Elapsed.TotalMilliseconds, "Milliseconds", ct: ct);
        await PublishPaymentEventAsync(payment, "PaymentInitiated", ct);

        logger.LogInformation("Payment initiated for order {OrderId} in {LatencyMs}ms", request.OrderId, stopwatch.Elapsed.TotalMilliseconds);

        return new InitiatePaymentResponseDto(payment.Id, payment.Code, payment.Status, payment.PaymentUrl);
    }

    private async Task PublishPaymentEventAsync(Domain.Payment payment, string eventType, CancellationToken ct)
    {
        if (_configuration == null)
        {
            return;
        }

        var payload = new
        {
            paymentId = payment.Id,
            paymentCode = payment.Code,
            orderId = payment.OrderId,
            userId = payment.UserId,
            amount = payment.Amount,
            status = payment.Status.ToString(),
            method = payment.Method.ToString(),
            eventType
        };

        try
        {
            var queueUrl = _configuration["AWS:SQS:PaymentEventsQueueUrl"]
                ?? _configuration["SQS:PaymentEventsQueueUrl"]
                ?? Environment.GetEnvironmentVariable("AWS_SQS_PAYMENT_EVENTS_QUEUE_URL");
            if (_sqsService != null && !string.IsNullOrWhiteSpace(queueUrl))
            {
                await _sqsService.SendMessageAsync(queueUrl, payload, ct);
            }

            var topicArn = _configuration["AWS:SNS:PaymentEventsTopicArn"]
                ?? _configuration["SNS:PaymentEventsTopicArn"]
                ?? Environment.GetEnvironmentVariable("AWS_SNS_PAYMENT_EVENTS_TOPIC_ARN");
            if (_snsService != null && !string.IsNullOrWhiteSpace(topicArn))
            {
                await _snsService.PublishAsync(topicArn, payload, eventType, ct);
            }

            if (_eventBridgeService != null)
            {
                var source = _configuration["AWS:EventBridge:PaymentEventSource"] ?? "e-verland.payments";
                await _eventBridgeService.PutEventAsync(source, eventType, payload, ct);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to publish payment event {EventType} for payment {PaymentId}", eventType, payment.Id);
        }
    }
}
