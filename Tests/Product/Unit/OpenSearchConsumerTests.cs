using Infra.AWS.CloudWatch;
using Infra.AWS.OpenSearch;
using Infra.AWS.SQS;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Modules.Product.Application.DTOs.Events;
using Modules.Product.Infrastructure.Services;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Tests.Product.Unit;

public class OpenSearchConsumerTests
{
    private const string QueueUrl = "https://sqs.us-east-1.amazonaws.com/123/product-sync";

    private static OpenSearchConsumer BuildConsumer(
        ISQSService sqsService,
        IOpenSearchService openSearch,
        ICloudWatchService cloudWatch)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SQS:ProductSyncQueueUrl"] = QueueUrl
            })
            .Build();

        return new OpenSearchConsumer(sqsService, openSearch, cloudWatch, config,
            NullLogger<OpenSearchConsumer>.Instance);
    }

    private static SQSMessage<ProductSyncEvent> MakeMessage(string eventType) =>
        new(
            MessageId: Guid.NewGuid().ToString(),
            ReceiptHandle: "receipt-" + Guid.NewGuid(),
            Body: new ProductSyncEvent
            {
                ProductId = Guid.NewGuid(),
                EventType = eventType,
                Name = "Test Product",
                Description = "Desc",
                BasePrice = 10m,
                VirtualPrice = 12m,
                Slug = "test-product",
                Status = "Active",
                CategoryIds = [],
                ImageUrls = [],
                Attributes = [],
                Timestamp = DateTime.UtcNow
            },
            ReceiveCount: 1,
            SentTimestamp: DateTime.UtcNow
        );

    /// <summary>
    /// Runs the consumer until it processes the given messages, then cancels.
    /// Sets up SQS to return messages on first call, then cancel on second call.
    /// </summary>
    private static async Task RunConsumerOnce(
        OpenSearchConsumer consumer,
        ISQSService sqsService,
        List<SQSMessage<ProductSyncEvent>> messages)
    {
        var cts = new CancellationTokenSource();
        var callCount = 0;

        sqsService.ReceiveMessagesAsync<ProductSyncEvent>(
                Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                callCount++;
                if (callCount == 1)
                    return Task.FromResult(messages);
                // Cancel after first batch is processed
                cts.Cancel();
                return Task.FromResult(new List<SQSMessage<ProductSyncEvent>>());
            });

        // Use IHostedService interface to start the background service
        await ((Microsoft.Extensions.Hosting.IHostedService)consumer).StartAsync(cts.Token);

        // Wait for processing to complete (with timeout)
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(2), cts.Token);
        }
        catch (OperationCanceledException) { }

        await ((Microsoft.Extensions.Hosting.IHostedService)consumer).StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ProcessMessage_Created_CallsIndexDocument()
    {
        var sqsService = Substitute.For<ISQSService>();
        var openSearch = Substitute.For<IOpenSearchService>();
        var cloudWatch = Substitute.For<ICloudWatchService>();
        var consumer = BuildConsumer(sqsService, openSearch, cloudWatch);

        var msg = MakeMessage("Created");
        await RunConsumerOnce(consumer, sqsService, [msg]);

        await openSearch.Received().IndexDocumentAsync(
            "products", msg.Body.ProductId.ToString(), Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessMessage_Updated_CallsIndexDocument()
    {
        var sqsService = Substitute.For<ISQSService>();
        var openSearch = Substitute.For<IOpenSearchService>();
        var cloudWatch = Substitute.For<ICloudWatchService>();
        var consumer = BuildConsumer(sqsService, openSearch, cloudWatch);

        var msg = MakeMessage("Updated");
        await RunConsumerOnce(consumer, sqsService, [msg]);

        await openSearch.Received().IndexDocumentAsync(
            "products", msg.Body.ProductId.ToString(), Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessMessage_Deleted_CallsDeleteDocument()
    {
        var sqsService = Substitute.For<ISQSService>();
        var openSearch = Substitute.For<IOpenSearchService>();
        var cloudWatch = Substitute.For<ICloudWatchService>();
        var consumer = BuildConsumer(sqsService, openSearch, cloudWatch);

        var msg = MakeMessage("Deleted");
        await RunConsumerOnce(consumer, sqsService, [msg]);

        await openSearch.Received().DeleteDocumentAsync(
            "products", msg.Body.ProductId.ToString(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessMessage_Success_DeletesMessageFromQueue()
    {
        var sqsService = Substitute.For<ISQSService>();
        var openSearch = Substitute.For<IOpenSearchService>();
        var cloudWatch = Substitute.For<ICloudWatchService>();
        var consumer = BuildConsumer(sqsService, openSearch, cloudWatch);

        var msg = MakeMessage("Created");
        await RunConsumerOnce(consumer, sqsService, [msg]);

        await sqsService.Received().DeleteMessageAsync(QueueUrl, msg.ReceiptHandle, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessMessage_WhenIndexFails_DoesNotDeleteMessage()
    {
        var sqsService = Substitute.For<ISQSService>();
        var openSearch = Substitute.For<IOpenSearchService>();
        var cloudWatch = Substitute.For<ICloudWatchService>();
        var consumer = BuildConsumer(sqsService, openSearch, cloudWatch);

        openSearch.IndexDocumentAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns<Task<string>>(_ => throw new Exception("OpenSearch unavailable"));

        var msg = MakeMessage("Created");
        await RunConsumerOnce(consumer, sqsService, [msg]);

        await sqsService.DidNotReceive().DeleteMessageAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
