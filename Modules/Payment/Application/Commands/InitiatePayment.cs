using System.Diagnostics;
using Amazon.XRay.Recorder.Core;
using Infra.AWS.CloudWatch;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Modules.Payment.Application.Contracts;
using Modules.Payment.Application.Helpers;
using Modules.Payment.Domain;
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
    IPaymentDbContext db,
    IProductReservationService reservationService,
    ISePayClient sePayClient,
    ICloudWatchService cloudWatch,
    ILogger<InitiatePaymentHandler> logger
) : IRequestHandler<InitiatePaymentCommand, InitiatePaymentResponseDto>
{
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

        // Wrap DB operations in X-Ray subsegment
        AWSXRayRecorder.Instance.BeginSubsegment("Payment.DB");
        try
        {
            // Reserve stock before persisting
            await reservationService.ReserveStockAsync(
                payment.Id,
                request.Items.Select(i => (i.SkuId, i.Quantity)),
                ct);

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

            try
            {
                await db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException ex)
            {
                throw new InvalidOperationException($"Payment creation failed due to database error: {ex.Message}");
            }
        }
        finally
        {
            AWSXRayRecorder.Instance.EndSubsegment();
        }

        stopwatch.Stop();

        // Emit CloudWatch metrics on success
        await cloudWatch.PutMetricAsync("payment.initiated", 1, "Count", ct: ct);
        await cloudWatch.PutMetricAsync("payment.latency_ms", stopwatch.Elapsed.TotalMilliseconds, "Milliseconds", ct: ct);

        logger.LogInformation("Payment initiated for order {OrderId} in {LatencyMs}ms", request.OrderId, stopwatch.Elapsed.TotalMilliseconds);

        return new InitiatePaymentResponseDto(payment.Id, payment.Code, payment.Status, payment.PaymentUrl);
    }
}
