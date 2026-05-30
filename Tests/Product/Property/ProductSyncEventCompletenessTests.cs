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
/// Property 8: For any Product entity, the ProductSyncEvent created from it has all
/// required fields non-null: ProductId, EventType, Name, Description, Slug, Status,
/// CategoryIds, ImageUrls, Attributes, Timestamp.
/// Validates: Requirements 3.4
/// </summary>
public class ProductSyncEventCompletenessTests
{
    private static Gen<Modules.Product.Domain.Product> GenProduct() =>
        from name in Gen.String[Gen.Char.AlphaNumeric, 1, 50]
        from description in Gen.String[Gen.Char.AlphaNumeric, 1, 200]
        from basePrice in Gen.Decimal[0.01m, 9999.99m]
        from virtualPrice in Gen.Decimal[0.01m, 9999.99m]
        from slug in Gen.String[Gen.Char.AlphaNumeric, 1, 50]
        from hasBrand in Gen.Bool
        from brandId in Gen.Guid
        select new Modules.Product.Domain.Product
        {
            Name = name,
            Description = description,
            BasePrice = basePrice,
            VirtualPrice = virtualPrice,
            Slug = slug,
            BrandId = hasBrand ? brandId : null,
            ImageUrls = [],
            Attributes = [],
            Categories = []
        };

    private static ProductSyncPublisher CreatePublisher(ISQSService sqsService, ICloudWatchService cloudWatch)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SQS:ProductSyncQueueUrl"] = "https://sqs.ap-southeast-1.amazonaws.com/123456789/test-queue"
            })
            .Build();

        return new ProductSyncPublisher(
            sqsService,
            cloudWatch,
            config,
            NullLogger<ProductSyncPublisher>.Instance);
    }

    /// <summary>
    /// Property 8: For any Product, the published ProductSyncEvent has all required fields non-null/non-default.
    /// </summary>
    [Fact]
    public void Property8_ProductSyncEvent_HasAllRequiredFieldsNonNull()
    {
        Gen.Select(GenProduct(), Gen.OneOf<string>(Gen.Const("Created"), Gen.Const("Updated"), Gen.Const("Deleted")))
            .Sample((product, eventType) =>
            {
                var sqsService = Substitute.For<ISQSService>();
                var cloudWatch = Substitute.For<ICloudWatchService>();

                ProductSyncEvent? capturedEvent = null;
                sqsService
                    .SendMessageAsync(Arg.Any<string>(), Arg.Do<ProductSyncEvent>(e => capturedEvent = e), Arg.Any<CancellationToken>())
                    .Returns(Task.FromResult("msg-id"));

                var publisher = CreatePublisher(sqsService, cloudWatch);
                publisher.PublishAsync(product, eventType).GetAwaiter().GetResult();

                Assert.NotNull(capturedEvent);

                // All required fields must be non-null / non-default
                Assert.NotEqual(Guid.Empty, capturedEvent.ProductId);
                Assert.NotNull(capturedEvent.EventType);
                Assert.NotEmpty(capturedEvent.EventType);
                Assert.NotNull(capturedEvent.Name);
                Assert.NotEmpty(capturedEvent.Name);
                Assert.NotNull(capturedEvent.Description);
                Assert.NotNull(capturedEvent.Slug);
                Assert.NotEmpty(capturedEvent.Slug);
                Assert.NotNull(capturedEvent.Status);
                Assert.NotEmpty(capturedEvent.Status);
                Assert.NotNull(capturedEvent.CategoryIds);
                Assert.NotNull(capturedEvent.ImageUrls);
                Assert.NotNull(capturedEvent.Attributes);
                Assert.NotEqual(default, capturedEvent.Timestamp);
            });
    }

    /// <summary>
    /// Property 8 (field mapping): ProductId matches product.Id, Status matches product.Status.ToString().
    /// </summary>
    [Fact]
    public void Property8_ProductSyncEvent_FieldsMappedCorrectly()
    {
        GenProduct().Sample(product =>
        {
            var sqsService = Substitute.For<ISQSService>();
            var cloudWatch = Substitute.For<ICloudWatchService>();

            ProductSyncEvent? capturedEvent = null;
            sqsService
                .SendMessageAsync(Arg.Any<string>(), Arg.Do<ProductSyncEvent>(e => capturedEvent = e), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult("msg-id"));

            var publisher = CreatePublisher(sqsService, cloudWatch);
            publisher.PublishAsync(product, "Created").GetAwaiter().GetResult();

            Assert.NotNull(capturedEvent);
            Assert.Equal(product.Id, capturedEvent.ProductId);
            Assert.Equal(product.Name, capturedEvent.Name);
            Assert.Equal(product.Description, capturedEvent.Description);
            Assert.Equal(product.BasePrice, capturedEvent.BasePrice);
            Assert.Equal(product.VirtualPrice, capturedEvent.VirtualPrice);
            Assert.Equal(product.Slug, capturedEvent.Slug);
            Assert.Equal(product.Status.ToString(), capturedEvent.Status);
            Assert.Equal(product.BrandId, capturedEvent.BrandId);
        });
    }
}
