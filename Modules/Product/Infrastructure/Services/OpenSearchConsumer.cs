using Infra.AWS.CloudWatch;
using Infra.AWS.SQS;
using Infra.Meilisearch;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Modules.Product.Application.DTOs.Events;

namespace Modules.Product.Infrastructure.Services;

public sealed class OpenSearchConsumer(
    ISQSService sqsService,
    IMeilisearchService meilisearchService,
    ICloudWatchService cloudWatch,
    IConfiguration configuration,
    ILogger<OpenSearchConsumer> logger) : BackgroundService
{
    private readonly ISQSService _sqsService = sqsService;
    private readonly IMeilisearchService _meilisearchService = meilisearchService;
    private readonly ICloudWatchService _cloudWatch = cloudWatch;
    private readonly IConfiguration _configuration = configuration;
    private readonly ILogger<OpenSearchConsumer> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var queueUrl = _configuration["AWS:SQS:ProductSyncQueueUrl"]
            ?? _configuration["SQS:ProductSyncQueueUrl"]
            ?? Environment.GetEnvironmentVariable("AWS_SQS_PRODUCT_SYNC_QUEUE_URL");
        if (string.IsNullOrWhiteSpace(queueUrl))
        {
            _logger.LogWarning("AWS:SQS:ProductSyncQueueUrl is not configured. ProductSync consumer will not start.");
            return;
        }

        _logger.LogInformation("ProductSync consumer started (Meilisearch). Polling queue: {QueueUrl}", queueUrl);

        while (!stoppingToken.IsCancellationRequested)
        {
            List<SQSMessage<ProductSyncEvent>> messages;
            try
            {
                messages = await _sqsService.ReceiveMessagesAsync<ProductSyncEvent>(queueUrl, ct: stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error receiving messages from SQS.");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                continue;
            }

            if (messages.Count == 0)
            {
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                continue;
            }

            var receiptHandlesToDelete = new List<string>(messages.Count);
            foreach (var message in messages)
            {
                var shouldDelete = await ProcessMessageAsync(queueUrl, message, stoppingToken);
                if (shouldDelete)
                {
                    receiptHandlesToDelete.Add(message.ReceiptHandle);
                }
            }

            if (receiptHandlesToDelete.Count > 0)
            {
                await _sqsService.DeleteMessageBatchAsync(queueUrl, receiptHandlesToDelete, stoppingToken);
            }
        }

        _logger.LogInformation("ProductSync consumer stopped.");
    }

    private async Task<bool> ProcessMessageAsync(string queueUrl, SQSMessage<ProductSyncEvent> message, CancellationToken ct)
    {
        var syncEvent = message.Body;
        var productId = syncEvent.ProductId.ToString();

        try
        {
            if (syncEvent.EventType is "Created" or "Updated")
            {
                var document = MapToProductDocument(syncEvent);
                await _meilisearchService.IndexDocumentAsync("products", productId, document, ct);
            }
            else if (syncEvent.EventType == "Deleted")
            {
                await _meilisearchService.DeleteDocumentAsync("products", productId, ct);
            }
            else if (syncEvent.EventType == "ProductModerated")
            {
                if (string.Equals(syncEvent.ModerationAction, "Delete", StringComparison.OrdinalIgnoreCase))
                {
                    await _meilisearchService.DeleteDocumentAsync("products", productId, ct);
                }
                else
                {
                    var document = MapToProductDocument(syncEvent);
                    await _meilisearchService.IndexDocumentAsync("products", productId, document, ct);
                }
            }
            else
            {
                _logger.LogWarning("Unknown EventType '{EventType}' for ProductId {ProductId}. Skipping.", syncEvent.EventType, productId);
                return false;
            }

            _logger.LogInformation("Product sync consumed. {ProductId} {EventType}", productId, syncEvent.EventType);
            await _cloudWatch.PutMetricAsync("product.sync.consumed", 1, "Count", ct: ct);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing product sync event. {ProductId} {EventType}", productId, syncEvent.EventType);

            var maxReceiveCountRaw = _configuration["AWS:SQS:MaxReceiveCount"]
                ?? _configuration["SQS:MaxReceiveCount"]
                ?? Environment.GetEnvironmentVariable("AWS_SQS_MAX_RECEIVE_COUNT");
            var maxReceiveCount = int.TryParse(maxReceiveCountRaw, out var parsedMaxReceiveCount)
                ? parsedMaxReceiveCount
                : 3;

            if (message.ReceiveCount >= maxReceiveCount)
            {
                var deadLetterQueueUrl = _configuration["AWS:SQS:ProductSyncDeadLetterQueueUrl"]
                    ?? _configuration["SQS:ProductSyncDeadLetterQueueUrl"]
                    ?? Environment.GetEnvironmentVariable("AWS_SQS_PRODUCT_SYNC_DLQ_URL");

                if (!string.IsNullOrWhiteSpace(deadLetterQueueUrl))
                {
                    await _sqsService.SendMessageAsync(deadLetterQueueUrl, syncEvent, ct);
                    _logger.LogWarning("Moved poison message {MessageId} to DLQ after {ReceiveCount} attempts.", message.MessageId, message.ReceiveCount);
                    return true;
                }
            }
        }

        return false;
    }

    private static ProductDocument MapToProductDocument(ProductSyncEvent syncEvent) => new()
    {
        Id = syncEvent.ProductId.ToString(),
        Name = syncEvent.Name,
        Description = syncEvent.Description,
        BasePrice = syncEvent.BasePrice,
        VirtualPrice = syncEvent.VirtualPrice,
        Slug = syncEvent.Slug,
        Status = syncEvent.Status,
        BrandId = syncEvent.BrandId?.ToString(),
        CategoryIds = syncEvent.CategoryIds.Select(id => id.ToString()).ToList(),
        ImageUrls = syncEvent.ImageUrls,
        Attributes = syncEvent.Attributes,
        IndexedAtUtc = DateTime.UtcNow
    };
}
