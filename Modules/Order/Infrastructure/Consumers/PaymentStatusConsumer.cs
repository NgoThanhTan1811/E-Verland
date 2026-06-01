using Infra.AWS.SQS;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Modules.Order.Application.Commands;
using Modules.Order.Domain;

namespace Modules.Order.Infrastructure.Consumers;

public sealed class PaymentStatusConsumer : BackgroundService
{
    private readonly ISQSService _sqsService;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PaymentStatusConsumer> _logger;
    private readonly int _pollIntervalMs = 5000;
    private readonly int _maxMessages = 10;

    public PaymentStatusConsumer(ISQSService sqsService, IServiceScopeFactory scopeFactory, IConfiguration configuration, ILogger<PaymentStatusConsumer> logger)
    {
        _sqsService = sqsService;
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var queueUrl = _configuration["AWS:SQS:PaymentEventsQueueUrl"]
            ?? _configuration["SQS:PaymentEventsQueueUrl"]
            ?? Environment.GetEnvironmentVariable("AWS_SQS_PAYMENT_EVENTS_QUEUE_URL");

        if (string.IsNullOrWhiteSpace(queueUrl))
        {
            _logger.LogWarning("PaymentEventsQueueUrl not configured, PaymentStatusConsumer will not run");
            return;
        }

        _logger.LogInformation("PaymentStatusConsumer started, listening to queue {QueueUrl}", queueUrl);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var messages = await _sqsService.ReceiveMessagesAsync<SharedKernel.Events.PaymentStatusChanged>(queueUrl, _maxMessages, stoppingToken);
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
                            _logger.LogWarning("Received null PaymentStatusChanged message");
                            await _sqsService.DeleteMessageAsync(queueUrl, msg.ReceiptHandle, stoppingToken);
                            continue;
                        }

                        // Map payment status to order status
                        OrderStatus targetStatus = body.NewStatus.ToLowerInvariant() switch
                        {
                            "success" => OrderStatus.Confirmed,
                            "failed" => OrderStatus.Canceled,
                            "refunded" => OrderStatus.Canceled,
                            _ => OrderStatus.Pending
                        };

                        using var scope = _scopeFactory.CreateScope();
                        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                        var command = new UpdateOrderStatusCommand(body.OrderId, targetStatus);
                        await mediator.Send(command, stoppingToken);

                        await _sqsService.DeleteMessageAsync(queueUrl, msg.ReceiptHandle, stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing PaymentStatusChanged message for payment {PaymentId}", msg.Body?.PaymentId);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error polling PaymentEvents queue");
                await Task.Delay(_pollIntervalMs, stoppingToken);
            }
        }

        _logger.LogInformation("PaymentStatusConsumer stopped");
    }
}
