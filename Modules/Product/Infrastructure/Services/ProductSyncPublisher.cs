using Infra.AWS.CloudWatch;
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
    ILogger<ProductSyncPublisher> logger) : IProductSyncPublisher
{
    private readonly ISQSService _sqsService = sqsService;
    private readonly ICloudWatchService _cloudWatch = cloudWatch;
    private readonly IConfiguration _configuration = configuration;
    private readonly ILogger<ProductSyncPublisher> _logger = logger;

    public async Task PublishAsync(Domain.Product product, string eventType, CancellationToken ct = default)
    {
        var queueUrl = _configuration["SQS:ProductSyncQueueUrl"];
        if (string.IsNullOrWhiteSpace(queueUrl))
        {
            _logger.LogWarning("SQS:ProductSyncQueueUrl is not configured. Skipping product sync publish.");
            return;
        }

        var syncEvent = new ProductSyncEvent
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
            Timestamp = DateTime.UtcNow
        };

        await _sqsService.SendMessageAsync(queueUrl, syncEvent, ct);

        _logger.LogInformation("Product sync event published. {ProductId} {EventType}", product.Id, eventType);
        await _cloudWatch.PutMetricAsync("product.sync.published", 1, "Count", ct: ct);
    }
}
