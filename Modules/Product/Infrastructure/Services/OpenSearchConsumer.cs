using Infra.AWS.CloudWatch;
using Infra.AWS.OpenSearch;
using Infra.AWS.SQS;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Modules.Product.Application.DTOs.Events;

namespace Modules.Product.Infrastructure.Services;

public sealed class OpenSearchConsumer(
    ISQSService sqsService,
    IOpenSearchService openSearchService,
    ICloudWatchService cloudWatch,
    IConfiguration configuration,
    ILogger<OpenSearchConsumer> logger) : BackgroundService
{
    private readonly ISQSService _sqsService = sqsService;
    private readonly IOpenSearchService _openSearchService = openSearchService;
    private readonly ICloudWatchService _cloudWatch = cloudWatch;
    private readonly IConfiguration _configuration = configuration;
    private readonly ILogger<OpenSearchConsumer> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var queueUrl = _configuration["SQS:ProductSyncQueueUrl"];
        if (string.IsNullOrWhiteSpace(queueUrl))
        {
            _logger.LogWarning("SQS:ProductSyncQueueUrl is not configured. OpenSearchConsumer will not start.");
            return;
        }

        _logger.LogInformation("OpenSearchConsumer started. Polling queue: {QueueUrl}", queueUrl);

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

            foreach (var message in messages)
            {
                await ProcessMessageAsync(queueUrl, message, stoppingToken);
            }
        }

        _logger.LogInformation("OpenSearchConsumer stopped.");
    }

    private async Task ProcessMessageAsync(string queueUrl, SQSMessage<ProductSyncEvent> message, CancellationToken ct)
    {
        var syncEvent = message.Body;
        var productId = syncEvent.ProductId.ToString();

        try
        {
            if (syncEvent.EventType is "Created" or "Updated")
            {
                var document = MapToProductDocument(syncEvent);
                await _openSearchService.IndexDocumentAsync("products", productId, document, ct);
            }
            else if (syncEvent.EventType == "Deleted")
            {
                await _openSearchService.DeleteDocumentAsync("products", productId, ct);
            }
            else
            {
                _logger.LogWarning("Unknown EventType '{EventType}' for ProductId {ProductId}. Skipping.", syncEvent.EventType, productId);
                return;
            }

            await _sqsService.DeleteMessageAsync(queueUrl, message.ReceiptHandle, ct);
            _logger.LogInformation("Product sync consumed. {ProductId} {EventType}", productId, syncEvent.EventType);
            await _cloudWatch.PutMetricAsync("product.sync.consumed", 1, "Count", ct: ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing product sync event. {ProductId} {EventType}", productId, syncEvent.EventType);
            // Do not delete message — it will be retried after visibility timeout
        }
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
