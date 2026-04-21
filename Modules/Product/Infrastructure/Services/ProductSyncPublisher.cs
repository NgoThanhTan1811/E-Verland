using Infra.AWS.CloudWatch;
using Infra.AWS.EventBridge;
using Infra.AWS.SNS;
using Infra.AWS.SQS;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Modules.Product.Application.Contracts;
using Modules.Product.Application.DTOs.Events;

namespace Modules.Product.Infrastructure.Services;

public sealed class ProductSyncPublisher(
    ISQSService sqsService,
    ICloudWatchService cloudWatch,
    IConfiguration configuration,
    ILogger<ProductSyncPublisher> logger,
    ISNSService? snsService = null,
    IEventBridgeService? eventBridgeService = null) : IProductSyncPublisher
{
    private readonly ISQSService _sqsService = sqsService;
    private readonly ICloudWatchService _cloudWatch = cloudWatch;
    private readonly IConfiguration _configuration = configuration;
    private readonly ILogger<ProductSyncPublisher> _logger = logger;
    private readonly ISNSService? _snsService = snsService;
    private readonly IEventBridgeService? _eventBridgeService = eventBridgeService;

    public async Task PublishAsync(Domain.Product product, string eventType, CancellationToken ct = default)
    {
        var syncEvent = BuildSyncEvent(product, eventType, null, null, null);
        await PublishCoreAsync(syncEvent, ct);
    }

    public async Task PublishModerationAsync(Domain.Product product, string action, Guid adminId, string reason, CancellationToken ct = default)
    {
        var syncEvent = BuildSyncEvent(product, "ProductModerated", action, adminId, reason);
        await PublishCoreAsync(syncEvent, ct);
    }

    private async Task PublishCoreAsync(ProductSyncEvent syncEvent, CancellationToken ct)
    {
        var queueUrl = _configuration["AWS:SQS:ProductSyncQueueUrl"]
            ?? _configuration["SQS:ProductSyncQueueUrl"]
            ?? Environment.GetEnvironmentVariable("AWS_SQS_PRODUCT_SYNC_QUEUE_URL");
        if (string.IsNullOrWhiteSpace(queueUrl))
        {
            _logger.LogWarning("AWS:SQS:ProductSyncQueueUrl is not configured. Skipping product sync publish.");
            return;
        }

        var topicArn = _configuration["AWS:SNS:ProductEventsTopicArn"]
            ?? _configuration["SNS:ProductEventsTopicArn"]
            ?? Environment.GetEnvironmentVariable("AWS_SNS_PRODUCT_EVENTS_TOPIC_ARN");

        await _sqsService.SendMessageAsync(queueUrl, syncEvent, ct);

        if (_snsService != null && !string.IsNullOrWhiteSpace(topicArn))
        {
            await _snsService.PublishAsync(topicArn, syncEvent, subject: $"Product{syncEvent.EventType}", ct: ct);
        }

        if (_eventBridgeService != null)
        {
            var source = _configuration["AWS:EventBridge:ProductEventSource"]
                ?? _configuration["EventBridge:ProductEventSource"]
                ?? "e-verland.products";

            await _eventBridgeService.PutEventAsync(source, syncEvent.EventType, syncEvent, ct);
        }

        _logger.LogInformation("Product sync event published. {ProductId} {EventType}", syncEvent.ProductId, syncEvent.EventType);
        await _cloudWatch.PutMetricAsync("product.sync.published", 1, "Count", ct: ct);
    }

    private static ProductSyncEvent BuildSyncEvent(
        Domain.Product product,
        string eventType,
        string? moderationAction,
        Guid? moderatedByAdminId,
        string? moderationReason)
    {
        return new ProductSyncEvent
        {
            ProductId = product.Id,
            EventType = eventType,
            Name = product.Name,
            Description = product.Description,
            BasePrice = product.BasePrice,
            VirtualPrice = product.VirtualPrice,
            Slug = product.Slug,
            Status = product.Status.ToString(),
            BrandId = product.BrandId,
            CategoryIds = product.Categories.Select(c => c.Id).ToList(),
            ImageUrls = product.ImageUrls,
            Attributes = product.Attributes,
            ModerationAction = moderationAction,
            ModeratedByAdminId = moderatedByAdminId,
            ModerationReason = moderationReason,
            Timestamp = DateTime.UtcNow
        };
    }
}
