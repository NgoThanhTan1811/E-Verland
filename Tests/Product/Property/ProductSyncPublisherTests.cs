using CsCheck;
using Infra.AWS.CloudWatch;
using Infra.AWS.SQS;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Modules.Product.Application.DTOs.Events;
using Modules.Product.Infrastructure.Services;
using NSubstitute;
using Xunit;

namespace Product.Property.Tests;

/// <summary>
/// Property 7: For any product data, each command (Create/Update/Delete) publishes
/// ProductSyncEvent with correct EventType and matching ProductId.
/// Validates: Requirements 3.1, 3.2, 3.3
/// </summary>
public class ProductSyncPublisherTests
{
    private static readonly string[] EventTypes = ["Created", "Updated", "Deleted"];

    private static Gen<Modules.Product.Domain.Product> GenProduct() =>
        from name in Gen.String[Gen.Char.AlphaNumeric, 1, 50]
        from description in Gen.String[Gen.Char.AlphaNumeric, 1, 200]
        from basePrice in Gen.Decimal[0.01m, 9999.99m]
        from virtualPrice in Gen.Decimal[0.01m, 9999.99m]
        from slug in Gen.String[Gen.Char.AlphaNumeric, 1, 50]
        select new Modules.Product.Domain.Product
        {
            Name = name,
            Description = description,
            BasePrice = basePrice,
            VirtualPrice = virtualPrice,
            Slug = slug,
            ImageUrls = [],
            Attributes = [],
            Categories = []
        };

    private static ProductSyncPublisher CreatePublisher(ISQSService sqsService, ICloudWatchService cloudWatch, string? queueUrl = "https://sqs.ap-southeast-1.amazonaws.com/123456789/test-queue")
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(queueUrl is null
                ? []
                : new Dictionary<string, string?> { ["SQS:ProductSyncQueueUrl"] = queueUrl })
            .Build();

        return new ProductSyncPublisher(
            sqsService,
            cloudWatch,
            config,
            NullLogger<ProductSyncPublisher>.Instance);
    }

    /// <summary>
    /// Property 7: For any product and any EventType, the published ProductSyncEvent
    /// has the correct EventType and ProductId matching the product.
    /// </summary>
    [Fact]
    public void Property7_PublishedEvent_HasCorrectEventTypeAndProductId()
    {
        Gen.Select(GenProduct(), Gen.Int[0, 2])
            .Sample((product, eventTypeIndex) =>
            {
                var eventType = EventTypes[eventTypeIndex];
                var sqsService = Substitute.For<ISQSService>();
                var cloudWatch = Substitute.For<ICloudWatchService>();

                ProductSyncEvent? capturedEvent = null;
                sqsService
                    .SendMessageAsync(Arg.Any<string>(), Arg.Do<ProductSyncEvent>(e => capturedEvent = e), Arg.Any<CancellationToken>())
                    .Returns(Task.FromResult("msg-id"));

                var publisher = CreatePublisher(sqsService, cloudWatch);
                publisher.PublishAsync(product, eventType).GetAwaiter().GetResult();

                Assert.NotNull(capturedEvent);
                Assert.Equal(eventType, capturedEvent.EventType);
                Assert.Equal(product.Id, capturedEvent.ProductId);
            });
    }

    /// <summary>
    /// Property 7 (graceful skip): When queue URL is not configured, no SQS call is made.
    /// </summary>
    [Fact]
    public void Property7_MissingQueueUrl_SkipsPublish()
    {
        GenProduct().Sample(product =>
        {
            var sqsService = Substitute.For<ISQSService>();
            var cloudWatch = Substitute.For<ICloudWatchService>();

            var publisher = CreatePublisher(sqsService, cloudWatch, queueUrl: null);
            publisher.PublishAsync(product, "Created").GetAwaiter().GetResult();

            sqsService.DidNotReceive().SendMessageAsync(
                Arg.Any<string>(),
                Arg.Any<ProductSyncEvent>(),
                Arg.Any<CancellationToken>());
        });
    }
}
