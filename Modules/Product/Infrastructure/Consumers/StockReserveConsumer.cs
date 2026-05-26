using Infra.AWS.SQS;
using Modules.Product.Application.Contracts;

namespace Modules.Product.Infrastructure.Consumers;

public sealed class StockReserveConsumer : BackgroundService
{
    private readonly ISQSService _sqsService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<StockReserveConsumer> _logger;

    private readonly IServiceScopeFactory _serviceScopeFactory;

    private readonly int _pollIntervalMs = 5000;
    private readonly int _maxMessages = 10;

    public StockReserveConsumer(
        ISQSService sqsService,
        IServiceScopeFactory serviceScopeFactory, 
        IConfiguration configuration,
        ILogger<StockReserveConsumer> logger)
    {
        _sqsService = sqsService;
        _serviceScopeFactory = serviceScopeFactory; 
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var queueUrl = _configuration["AWS:SQS:StockReserveQueueUrl"]
            ?? _configuration["SQS:StockReserveQueueUrl"]
            ?? Environment.GetEnvironmentVariable("AWS_SQS_STOCK_RESERVE_QUEUE_URL");

        if (string.IsNullOrWhiteSpace(queueUrl))
        {
            _logger.LogWarning("StockReserveQueueUrl not configured, StockReserveConsumer will not run");
            return;
        }

        _logger.LogInformation("StockReserveConsumer started, listening to queue {QueueUrl}", queueUrl);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var messages = await _sqsService.ReceiveMessagesAsync<SharedKernel.Events.StockReserveRequested>(queueUrl, _maxMessages, stoppingToken);
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
                            _logger.LogWarning("Received null StockReserveRequested message");
                            await _sqsService.DeleteMessageAsync(queueUrl, msg.ReceiptHandle, stoppingToken);
                            continue;
                        }

                        using (var scope = _serviceScopeFactory.CreateScope())
                        {
                            var reservationService = scope.ServiceProvider
                                .GetRequiredService<IProductReservationService>();

                            await reservationService.ReserveStockAsync(body.OrderId, body.PaymentId, body.Items, stoppingToken);
                        }

                        await _sqsService.DeleteMessageAsync(queueUrl, msg.ReceiptHandle, stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing StockReserveRequested message");
                        // Leave message for retry
                    }
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error polling StockReserve queue");
                await Task.Delay(_pollIntervalMs, stoppingToken);
            }
        }

        _logger.LogInformation("StockReserveConsumer stopped");
    }
}