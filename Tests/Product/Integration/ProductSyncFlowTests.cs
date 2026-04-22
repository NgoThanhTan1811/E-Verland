using Amazon.XRay.Recorder.Core;
using Infra.AWS.CloudWatch;
using Infra.AWS.SQS;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Modules.Product.Application.Commands;
using Modules.Product.Application.Contracts;
using Modules.Product.Application.DTOs.Events;
using Modules.Product.Application.DTOs.Request;
using Modules.Product.Application.Services;
using Modules.Product.Domain;
using Modules.Product.Infrastructure.Services;
using NSubstitute;
using Xunit;

namespace Tests.Product.Integration;

/// <summary>
/// Integration-style tests verifying the full flow from command handler → ProductSyncPublisher → SQS.
/// Uses a real ProductSyncPublisher with a mocked ISQSService.
/// </summary>
public class ProductSyncFlowTests
{
    private const string QueueUrl = "https://sqs.us-east-1.amazonaws.com/123/product-sync";

    private static ProductSyncPublisher CreateRealPublisher(ISQSService sqsService, ICloudWatchService cloudWatch)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SQS:ProductSyncQueueUrl"] = QueueUrl
            })
            .Build();

        return new ProductSyncPublisher(sqsService, cloudWatch, config,
            NullLogger<ProductSyncPublisher>.Instance);
    }

    private static void WithXRay(Action action)
    {
        AWSXRayRecorder.Instance.BeginSegment("test");
        try { action(); }
        finally { AWSXRayRecorder.Instance.EndSegment(); }
    }

    private static CreateProductRequestDto SampleCreateRequest() => new()
    {
        Name = "Test Product",
        Description = "A test product",
        BasePrice = 99.99m,
        VirtualPrice = 119.99m,
        Slug = "test-product",
        ImageUrls = [],
        Attributes = [],
        CategoryIds = [],
        Status = ProductStatus.Published    
    };

    [Fact]
    public void CreateProduct_PublishesCreatedEvent_ToSQS()
    {
        var sqsService = Substitute.For<ISQSService>();
        var cloudWatch = Substitute.For<ICloudWatchService>();
        var productRepo = Substitute.For<IProductRepository>();
        var categoryRepo = Substitute.For<ICategoryRepository>();
        var skuRepo = Substitute.For<ISkuRepository>();
        var dbContext = Substitute.For<IProductDbContext>();
        var skuGenerator = new SKUGeneratorService();
        var syncPublisher = CreateRealPublisher(sqsService, cloudWatch);

        sqsService.SendMessageAsync(Arg.Any<string>(), Arg.Any<ProductSyncEvent>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult("msg-id"));
        productRepo.CreateAsync(Arg.Any<Modules.Product.Domain.Product>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        dbContext.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
        cloudWatch.PutMetricAsync(Arg.Any<string>(), Arg.Any<double>(), Arg.Any<string>(),
            Arg.Any<Dictionary<string, string>>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        var handler = new CreateProduc(productRepo, categoryRepo, skuRepo, dbContext,
            skuGenerator, syncPublisher, cloudWatch);

        ProductSyncEvent? captured = null;
        sqsService.When(s => s.SendMessageAsync(QueueUrl, Arg.Any<ProductSyncEvent>(), Arg.Any<CancellationToken>()))
            .Do(ci => captured = ci.ArgAt<ProductSyncEvent>(1));

        WithXRay(() =>
        {
            handler.Handle(new CreateProductCommand(SampleCreateRequest()), CancellationToken.None)
                .GetAwaiter().GetResult();
        });

        Assert.NotNull(captured);
        Assert.Equal("Created", captured.EventType);
    }

    [Fact]
    public void UpdateProduct_PublishesUpdatedEvent_ToSQS()
    {
        var sqsService = Substitute.For<ISQSService>();
        var cloudWatch = Substitute.For<ICloudWatchService>();
        var productRepo = Substitute.For<IProductRepository>();
        var categoryRepo = Substitute.For<ICategoryRepository>();
        var dbContext = Substitute.For<IProductDbContext>();
        var syncPublisher = CreateRealPublisher(sqsService, cloudWatch);

        var existingProduct = new Modules.Product.Domain.Product
        {
            Name = "Old Name",
            Description = "Old Desc",
            BasePrice = 10m,
            VirtualPrice = 12m,
            Slug = "old-slug",
            ImageUrls = [],
            Attributes = [],
            Categories = []
        };

        sqsService.SendMessageAsync(Arg.Any<string>(), Arg.Any<ProductSyncEvent>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult("msg-id"));
        productRepo.GetByIdAsync(existingProduct.Id, Arg.Any<CancellationToken>()).Returns(existingProduct);
        productRepo.UpdateAsync(Arg.Any<Modules.Product.Domain.Product>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        dbContext.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
        cloudWatch.PutMetricAsync(Arg.Any<string>(), Arg.Any<double>(), Arg.Any<string>(),
            Arg.Any<Dictionary<string, string>>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        var handler = new UpdateProductHandler(productRepo, categoryRepo, dbContext, syncPublisher, cloudWatch);

        ProductSyncEvent? captured = null;
        sqsService.When(s => s.SendMessageAsync(QueueUrl, Arg.Any<ProductSyncEvent>(), Arg.Any<CancellationToken>()))
            .Do(ci => captured = ci.ArgAt<ProductSyncEvent>(1));

        var updateRequest = new UpdateProductRequestDto
        {
            Name = "New Name",
            Description = "New Desc",
            BasePrice = 20m,
            VirtualPrice = 25m,
            Slug = "new-slug",
            ImageUrls = [],
            Attributes = [],
            CategoryIds = [],
            Status = ProductStatus.Published
        };

        WithXRay(() =>
        {
            handler.Handle(new UpdateProductCommand(existingProduct.Id, updateRequest), CancellationToken.None)
                .GetAwaiter().GetResult();
        });

        Assert.NotNull(captured);
        Assert.Equal("Updated", captured.EventType);
    }

    [Fact]
    public void DeleteProduct_PublishesDeletedEvent_ToSQS()
    {
        var sqsService = Substitute.For<ISQSService>();
        var cloudWatch = Substitute.For<ICloudWatchService>();
        var productRepo = Substitute.For<IProductRepository>();
        var dbContext = Substitute.For<IProductDbContext>();
        var syncPublisher = CreateRealPublisher(sqsService, cloudWatch);

        var existingProduct = new Modules.Product.Domain.Product
        {
            Name = "To Delete",
            Description = "Desc",
            BasePrice = 5m,
            VirtualPrice = 6m,
            Slug = "to-delete",
            ImageUrls = [],
            Attributes = [],
            Categories = []
        };

        sqsService.SendMessageAsync(Arg.Any<string>(), Arg.Any<ProductSyncEvent>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult("msg-id"));
        productRepo.GetByIdAsync(existingProduct.Id, Arg.Any<CancellationToken>()).Returns(existingProduct);
        productRepo.DeleteAsync(existingProduct.Id, Arg.Any<CancellationToken>()).Returns(true);
        dbContext.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
        cloudWatch.PutMetricAsync(Arg.Any<string>(), Arg.Any<double>(), Arg.Any<string>(),
            Arg.Any<Dictionary<string, string>>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        var handler = new DeleteProductHandler(productRepo, dbContext, syncPublisher, cloudWatch);

        ProductSyncEvent? captured = null;
        sqsService.When(s => s.SendMessageAsync(QueueUrl, Arg.Any<ProductSyncEvent>(), Arg.Any<CancellationToken>()))
            .Do(ci => captured = ci.ArgAt<ProductSyncEvent>(1));

        WithXRay(() =>
        {
            handler.Handle(new DeleteProductCommand(existingProduct.Id), CancellationToken.None)
                .GetAwaiter().GetResult();
        });

        Assert.NotNull(captured);
        Assert.Equal("Deleted", captured.EventType);
    }
}
