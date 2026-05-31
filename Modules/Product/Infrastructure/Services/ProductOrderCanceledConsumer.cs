using Infra.AWS.SQS;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Modules.Order.Application.DTOs.Events;
using Modules.Product.Application.Commands;

namespace Modules.Product.Infrastructure.Services;

/// <summary>
/// Background service that consumes OrderCanceled events from SQS
/// This decouples Order and Product modules - Product Module reacts to order cancellations
/// by releasing stock reservations asynchronously
/// </summary>
public sealed class ProductOrderCanceledConsumer : BackgroundService
{
    private readonly ISQSService _sqsService;
    private readonly IMediator _mediator;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ProductOrderCanceledConsumer> _logger;
    private readonly TimeSpan _pollInterval = TimeSpan.FromSeconds(5);
    private readonly int _maxMessages = 10;

    public ProductOrderCanceledConsumer(
        ISQSService sqsService,
        IMediator mediator,
        IConfiguration configuration,
        ILogger<ProductOrderCanceledConsumer> logger)
    {
        _sqsService = sqsService;
        _mediator = mediator;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var queueUrl = _configuration["AWS:SQS:OrderEventsQueueUrl"]
            ?? _configuration["SQS:OrderEventsQueueUrl"]
            ?? Environment.GetEnvironmentVariable("AWS_SQS_ORDER_EVENTS_QUEUE_URL");

        if (string.IsNullOrWhiteSpace(queueUrl))
        {
            _logger.LogWarning("OrderEventsQueueUrl not configured, ProductOrderCanceledConsumer will not run");
            return;
        }

        _logger.LogInformation("ProductOrderCanceledConsumer started, listening to queue {QueueUrl}", queueUrl);

        using var timer = new PeriodicTimer(_pollInterval);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    await PollAndProcessMessagesAsync(queueUrl, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in ProductOrderCanceledConsumer");
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }

        _logger.LogInformation("ProductOrderCanceledConsumer stopped");
    }

    private async Task PollAndProcessMessagesAsync(string queueUrl, CancellationToken stoppingToken)
    {
        try
        {
            var messages = await _sqsService.ReceiveMessagesAsync<OrderCanceledEvent>(
                queueUrl, _maxMessages, stoppingToken);

            if (messages == null || messages.Count == 0)
            {
                return;
            }

            foreach (var message in messages)
            {
                try
                {
                    await ProcessOrderCanceledEventAsync(message, stoppingToken);
                    await _sqsService.DeleteMessageAsync(queueUrl, message.ReceiptHandle, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing OrderCanceled event for payment {PaymentId}",
                        message.Body?.PaymentId);
                    // Message will be re-queued after visibility timeout
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            throw;
        }
        catch (TaskCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            throw new OperationCanceledException(stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error receiving messages from SQS queue {QueueUrl}", queueUrl);
        }
    }

    private async Task ProcessOrderCanceledEventAsync(
        SQSMessage<OrderCanceledEvent> message,
        CancellationToken stoppingToken)
    {
        var orderCanceledEvent = message.Body;

        if (orderCanceledEvent == null)
        {
            _logger.LogWarning("Received null OrderCanceledEvent from SQS");
            return;
        }

        if (!orderCanceledEvent.PaymentId.HasValue)
        {
            _logger.LogInformation(
                "Order {OrderCode} (ID: {OrderId}) canceled but has no payment, nothing to release",
                orderCanceledEvent.OrderCode, orderCanceledEvent.OrderId);
            return;
        }

        _logger.LogInformation(
            "Processing OrderCanceled event for order {OrderCode}, releasing payment {PaymentId}",
            orderCanceledEvent.OrderCode, orderCanceledEvent.PaymentId);

        var command = new ReleaseStockReservationCommand(
            orderCanceledEvent.PaymentId.Value,
            orderCanceledEvent.OrderCode,
            "order-canceled-event");

        var result = await _mediator.Send(command, stoppingToken);

        if (result)
        {
            _logger.LogInformation(
                "Successfully processed OrderCanceled event for order {OrderCode}",
                orderCanceledEvent.OrderCode);
        }
        else
        {
            _logger.LogWarning(
                "Failed to process OrderCanceled event for order {OrderCode}, will retry",
                orderCanceledEvent.OrderCode);
            // Rethrow so SQS will re-queue the message
            throw new InvalidOperationException(
                $"Failed to release stock reservation for payment {orderCanceledEvent.PaymentId}");
        }
    }
}
