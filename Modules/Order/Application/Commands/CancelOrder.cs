using Infra.AWS.CloudWatch;
using Infra.AWS.SNS;
using Infra.AWS.SQS;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Modules.Order.Application.Contracts;
using Modules.Order.Application.DTOs.Events;
using Modules.Order.Domain;
using SharedKernel.Events;

namespace Modules.Order.Application.Commands;

public sealed record CancelOrderCommand(
    Guid OrderId,
    Guid UserId
) : IRequest<Unit>;

public sealed class CancelOrderHandler(
    IOrderRepository repo,
    IOrderDbContext db,
    ICloudWatchService cloudWatch,
    ISNSService snsService,
    ISQSService sqsService,
    IConfiguration configuration,
    ILogger<CancelOrderHandler> logger)
    : IRequestHandler<CancelOrderCommand, Unit>
{
    private readonly IOrderRepository _repo = repo;
    private readonly IOrderDbContext _db = db;
    private readonly ICloudWatchService _cloudWatch = cloudWatch;
    private readonly ISNSService _snsService = snsService;
    private readonly ISQSService _sqsService = sqsService;
    private readonly IConfiguration _configuration = configuration;
    private readonly ILogger<CancelOrderHandler> _logger = logger;

    public async Task<Unit> Handle(CancelOrderCommand request, CancellationToken ct)
    {
        var order = await _repo.GetByIdAsync(request.OrderId, ct)
            ?? throw new KeyNotFoundException("Order not found");

        if (order.UserId != request.UserId)
            throw new UnauthorizedAccessException("You can only cancel your own orders");

        if (order.Status == OrderStatus.Canceled)
            throw new InvalidOperationException("Order is already canceled");

        if (order.Status == OrderStatus.Completed)
            throw new InvalidOperationException("Cannot cancel a completed order");

        if (order.Status == OrderStatus.Shipping)
            throw new InvalidOperationException("Cannot cancel an order that is being shipped");

        order.Status = OrderStatus.Canceled;

        await _repo.UpdateAsync(order, ct);

        using var transaction = await _db.BeginTransactionAsync(ct);
        try
        {
            order.Status = OrderStatus.Canceled;
            await _repo.UpdateAsync(order, ct);
            await _db.SaveChangesAsync(ct);

            // Publish OrderCanceledEvent to SNS/SQS for Product Module to consume
            // Product Module will handle releasing stock reservations asynchronously
            await PublishOrderCanceledEventAsync(order, ct);

            await transaction.CommitAsync(ct);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(ct);
            _logger.LogError(ex, "Failed to cancel order {OrderId}", request.OrderId);
            throw new InvalidOperationException("Failed to cancel order.", ex);
        }

        await _cloudWatch.PutMetricAsync("order.cancelled", 1, "Count", ct: ct);
        return Unit.Value;
    }

    private async Task PublishOrderCanceledEventAsync(Domain.Order order, CancellationToken ct)
    {
        var orderCanceledEvent = new OrderCanceledEvent
        {
            OrderId = order.Id,
            UserId = order.UserId,
            PaymentId = order.PaymentId,
            OrderCode = order.Code,
            TotalPrice = order.TotalPrice,
            CanceledAtUtc = DateTime.UtcNow
        };

        try
        {
            // Try publishing via SNS first (if configured)
            var snsTopicArn = _configuration["AWS:SNS:OrderEventsTopicArn"]
                ?? _configuration["SNS:OrderEventsTopicArn"]
                ?? Environment.GetEnvironmentVariable("AWS_SNS_ORDER_EVENTS_TOPIC_ARN");

            if (!string.IsNullOrWhiteSpace(snsTopicArn))
            {
                await _snsService.PublishAsync(snsTopicArn, orderCanceledEvent, "OrderCanceled", ct);
                _logger.LogInformation("Published OrderCanceled event for order {OrderId} via SNS", order.Id);
            }

            // Also publish to SQS (if configured) so Product Module can subscribe
            var sqsQueueUrl = _configuration["AWS:SQS:OrderEventsQueueUrl"]
                ?? _configuration["SQS:OrderEventsQueueUrl"]
                ?? Environment.GetEnvironmentVariable("AWS_SQS_ORDER_EVENTS_QUEUE_URL");

            if (!string.IsNullOrWhiteSpace(sqsQueueUrl))
            {
                await _sqsService.SendMessageAsync(sqsQueueUrl, orderCanceledEvent, ct);
                _logger.LogInformation("Published OrderCanceled event for order {OrderId} via SQS", order.Id);
            }

            await PublishShippingCancelRequestedAsync(order, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to publish OrderCanceled event for order {OrderId}, but order was canceled successfully", order.Id);
            // Don't rethrow - the order was already canceled in DB
            // Product Module will eventually release reservations on next sync
        }
    }

    private async Task PublishShippingCancelRequestedAsync(Domain.Order order, CancellationToken ct)
    {
        var queueUrl = _configuration["AWS:SQS:ShippingDraftQueueUrl"]
            ?? _configuration["SQS:ShippingDraftQueueUrl"]
            ?? Environment.GetEnvironmentVariable("AWS_SQS_SHIPPING_DRAFT_QUEUE_URL");

        if (string.IsNullOrWhiteSpace(queueUrl))
        {
            return;
        }

        try
        {
            var evt = new ShippingCancelRequested(order.Id, "Order canceled");
            await _sqsService.SendMessageAsync(queueUrl, evt, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to publish ShippingCancelRequested for order {OrderId}", order.Id);
        }
    }
}
