using Infra.AWS.SQS;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Modules.Order.Application.Commands;
using Modules.Order.Domain;
using SharedKernel.Events;

namespace Modules.Order.Infrastructure.Consumers;

public sealed class ShippingStatusConsumer : BackgroundService
{
    private readonly ISQSService _sqsService;
    private readonly IMediator _mediator;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ShippingStatusConsumer> _logger;
    private readonly int _pollIntervalMs = 5000;
    private readonly int _maxMessages = 10;

    public ShippingStatusConsumer(
        ISQSService sqsService,
        IMediator mediator,
        IConfiguration configuration,
        ILogger<ShippingStatusConsumer> logger)
    {
        _sqsService = sqsService;
        _mediator = mediator;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var queueUrl = _configuration["AWS:SQS:ShippingStatusQueueUrl"]
            ?? _configuration["SQS:ShippingStatusQueueUrl"]
            ?? Environment.GetEnvironmentVariable("AWS_SQS_SHIPPING_STATUS_QUEUE_URL");

        if (string.IsNullOrWhiteSpace(queueUrl))
        {
            _logger.LogWarning("ShippingStatusQueueUrl not configured, ShippingStatusConsumer will not run");
            return;
        }

        _logger.LogInformation("ShippingStatusConsumer started, listening to queue {QueueUrl}", queueUrl);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var messages = await _sqsService.ReceiveMessagesAsync<ShippingStatusChanged>(queueUrl, _maxMessages, stoppingToken);
                if (messages == null || messages.Count == 0)
                {
                    await Task.Delay(_pollIntervalMs, stoppingToken);
                    continue;
                }

                foreach (var msg in messages)
                {
                    try
                    {
                        var body = msg.Body;
                        if (body == null)
                        {
                            _logger.LogWarning("Received null ShippingStatusChanged message");
                            await _sqsService.DeleteMessageAsync(queueUrl, msg.ReceiptHandle, stoppingToken);
                            continue;
                        }

                        var targetStatus = MapOrderStatus(body.Status);
                        if (targetStatus == null)
                        {
                            _logger.LogInformation("Ignoring shipping status {Status} for order {OrderId}", body.Status, body.OrderId);
                            await _sqsService.DeleteMessageAsync(queueUrl, msg.ReceiptHandle, stoppingToken);
                            continue;
                        }

                        var command = new UpdateOrderStatusCommand(body.OrderId, targetStatus.Value);
                        await _mediator.Send(command, stoppingToken);

                        await _sqsService.DeleteMessageAsync(queueUrl, msg.ReceiptHandle, stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing ShippingStatusChanged message for order {OrderId}", msg.Body?.OrderId);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error polling ShippingStatus queue");
                await Task.Delay(_pollIntervalMs, stoppingToken);
            }
        }

        _logger.LogInformation("ShippingStatusConsumer stopped");
    }

    private static OrderStatus? MapOrderStatus(string status)
    {
        var normalized = status?.Trim().ToLowerInvariant() ?? string.Empty;

        return normalized switch
        {
            "ready_to_pick" => OrderStatus.Shipping,
            "picking" => OrderStatus.Shipping,
            "picked" => OrderStatus.Shipping,
            "storing" => OrderStatus.Shipping,
            "transporting" => OrderStatus.Shipping,
            "delivering" => OrderStatus.Shipping,
            "created" => OrderStatus.Shipping,
            "delivered" => OrderStatus.Completed,
            "return" => OrderStatus.Canceled,
            "returned" => OrderStatus.Canceled,
            "cancel" => OrderStatus.Canceled,
            "canceled" => OrderStatus.Canceled,
            "cancelled" => OrderStatus.Canceled,
            _ => null
        };
    }
}
