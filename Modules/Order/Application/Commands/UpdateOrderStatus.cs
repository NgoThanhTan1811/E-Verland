using AutoMapper;
using Infra.AWS.EventBridge;
using Infra.AWS.SNS;
using Infra.AWS.SQS;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Modules.Order.Application.Contracts;
using Modules.Order.Application.DTOs.Response;
using Modules.Order.Domain;

namespace Modules.Order.Application.Commands;

public sealed record UpdateOrderStatusCommand(
    Guid OrderId,
    OrderStatus Status
) : IRequest<OrderOverviewResponseDto>;

public sealed class UpdateOrderStatusHandler(
    IOrderRepository repo,
    IOrderDbContext db,
    IMapper mapper,
    ILogger<UpdateOrderStatusHandler> logger,
    IConfiguration? configuration = null,
    ISQSService? sqsService = null,
    ISNSService? snsService = null,
    IEventBridgeService? eventBridgeService = null)
            : IRequestHandler<UpdateOrderStatusCommand, OrderOverviewResponseDto>
{
    private readonly IOrderRepository _repo = repo;
    private readonly IOrderDbContext _db = db;
    private readonly IMapper _mapper = mapper;
    private readonly ILogger<UpdateOrderStatusHandler> _logger = logger;
    private readonly IConfiguration? _configuration = configuration;
    private readonly ISQSService? _sqsService = sqsService;
    private readonly ISNSService? _snsService = snsService;
    private readonly IEventBridgeService? _eventBridgeService = eventBridgeService;

    public async Task<OrderOverviewResponseDto> Handle(UpdateOrderStatusCommand request, CancellationToken ct)
    {
        var order = await _repo.GetByIdAsync(request.OrderId, ct)
            ?? throw new KeyNotFoundException("Order not found");

        if (order.Status == OrderStatus.Canceled)
            throw new InvalidOperationException("Cannot update a canceled order");

        if (order.Status == OrderStatus.Completed)
            throw new InvalidOperationException("Cannot update a completed order");

        if (!IsValidTransition(order.Status, request.Status))
            throw new InvalidOperationException($"Invalid status transition: {order.Status} -> {request.Status}");

        order.Status = request.Status;

        await _repo.UpdateAsync(order, ct);

        try
        {
            await _db.SaveChangesAsync(ct);
            await PublishOrderStatusEventAsync(order, request.Status, ct);
        }
        catch (DbUpdateException)
        {
            throw new InvalidOperationException("Order status update failed due to database error.");
        }

        return _mapper.Map<OrderOverviewResponseDto>(order);
    }

    private static bool IsValidTransition(OrderStatus current, OrderStatus next)
    {
        if (current == next)
            return true;

        return current switch
        {
            OrderStatus.Pending => next is OrderStatus.Confirmed or OrderStatus.Canceled,
            OrderStatus.Confirmed => next is OrderStatus.Shipping,
            OrderStatus.Shipping => next is OrderStatus.Completed,
            _ => false
        };
    }

    private async Task PublishOrderStatusEventAsync(Domain.Order order, OrderStatus status, CancellationToken ct)
    {
        if (_configuration == null)
        {
            return;
        }

        var eventType = status switch
        {
            OrderStatus.Confirmed => "OrderConfirmed",
            OrderStatus.Shipping => "OrderShipping",
            OrderStatus.Completed => "OrderCompleted",
            OrderStatus.Canceled => "OrderCanceled",
            _ => $"Order{status}"
        };

        var payload = new
        {
            orderId = order.Id,
            orderCode = order.Code,
            userId = order.UserId,
            status = status.ToString(),
            paymentStatus = order.PaymentStatus.ToString(),
            updatedAtUtc = DateTime.UtcNow,
            eventType
        };

        try
        {
            var queueUrl = _configuration["AWS:SQS:OrderEventsQueueUrl"]
                ?? _configuration["SQS:OrderEventsQueueUrl"]
                ?? Environment.GetEnvironmentVariable("AWS_SQS_ORDER_EVENTS_QUEUE_URL");
            if (_sqsService != null && !string.IsNullOrWhiteSpace(queueUrl))
            {
                await _sqsService.SendMessageAsync(queueUrl, payload, ct);
            }

            var topicArn = _configuration["AWS:SNS:OrderEventsTopicArn"]
                ?? _configuration["SNS:OrderEventsTopicArn"]
                ?? Environment.GetEnvironmentVariable("AWS_SNS_ORDER_EVENTS_TOPIC_ARN");
            if (_snsService != null && !string.IsNullOrWhiteSpace(topicArn))
            {
                await _snsService.PublishAsync(topicArn, payload, eventType, ct);
            }

            if (_eventBridgeService != null)
            {
                var source = _configuration["AWS:EventBridge:OrderEventSource"] ?? "e-verland.orders";
                await _eventBridgeService.PutEventAsync(source, eventType, payload, ct);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to publish order status event {EventType} for order {OrderId}", eventType, order.Id);
        }
    }
}
