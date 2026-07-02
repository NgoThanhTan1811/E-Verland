using AutoMapper;
using Infra.AWS.EventBridge;
using Infra.AWS.SNS;
using Infra.AWS.SQS;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Modules.Payment.Application.Contracts;
using Modules.Payment.Application.DTOs.Response;
using Modules.Payment.Domain;

namespace Modules.Payment.Application.Commands;

public sealed record UpdatePaymentStatusCommand(
    Guid PaymentId,
    PaymentStatus Status
) : IRequest<PaymentResponseDto>;

public sealed class UpdatePaymentStatusHandler(
    IPaymentRepository repo,
    IPaymentDbContext db,
    IMapper mapper,
    ILogger<UpdatePaymentStatusHandler> logger,
    IConfiguration? configuration = null,
    ISQSService? sqsService = null,
    ISNSService? snsService = null,
    IEventBridgeService? eventBridgeService = null) : IRequestHandler<UpdatePaymentStatusCommand, PaymentResponseDto>
{
    private readonly IPaymentRepository _repo = repo;
    private readonly IPaymentDbContext _db = db;
    private readonly IMapper _mapper = mapper;
    private readonly ILogger<UpdatePaymentStatusHandler> _logger = logger;
    private readonly IConfiguration? _configuration = configuration;
    private readonly ISQSService? _sqsService = sqsService;
    private readonly ISNSService? _snsService = snsService;
    private readonly IEventBridgeService? _eventBridgeService = eventBridgeService;

    public async Task<PaymentResponseDto> Handle(UpdatePaymentStatusCommand request, CancellationToken ct)
    {
        var payment = await _repo.GetByIdAsync(request.PaymentId, ct)
            ?? throw new KeyNotFoundException("Payment not found");

        if (payment.Status == PaymentStatus.Success && request.Status != PaymentStatus.Refunded)
            throw new InvalidOperationException("Only Refund operation is allowed for a successful payment.");

        if (payment.Status == PaymentStatus.Refunded)
            throw new InvalidOperationException("Cannot update status of a refunded payment");

        payment.Status = request.Status;

        await _repo.UpdateAsync(payment, ct);

        try
        {
            await _db.SaveChangesAsync(ct);
            // Order status sync is handled asynchronously by PaymentStatusConsumer
            // in the Order module via the PaymentEvents SQS queue — no direct call needed.
            await PublishPaymentStatusEventAsync(payment, request.Status, ct);
        }
        catch (DbUpdateException)
        {
            throw new InvalidOperationException("Payment status update failed due to database error.");
        }

        return _mapper.Map<PaymentResponseDto>(payment);
    }

    private async Task PublishPaymentStatusEventAsync(Domain.Payment payment, PaymentStatus status, CancellationToken ct)
    {
        if (_configuration == null)
        {
            return;
        }

        var eventType = status switch
        {
            PaymentStatus.Success => "PaymentSuccess",
            PaymentStatus.Failed => "PaymentFailed",
            PaymentStatus.Refunded => "PaymentRefunded",
            _ => $"Payment{status}"
        };

        var payload = new
        {
            PaymentId = payment.Id,
            PaymentCode = payment.Code,
            OrderId = payment.OrderId,
            UserId = payment.UserId,
            Amount = payment.Amount,
            NewStatus = payment.Status.ToString(),
            Method = payment.Method.ToString(),
            EventType = eventType,
            OccurredAt = DateTime.UtcNow
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
            _logger.LogWarning(ex, "Failed to publish payment status event {EventType} for payment {PaymentId}", eventType, payment.Id);
        }
    }
}
