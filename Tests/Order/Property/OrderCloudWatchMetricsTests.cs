using Amazon.XRay.Recorder.Core;
using AutoMapper;
using CsCheck;
using Infra.AWS.CloudWatch;
using Microsoft.Extensions.Logging.Abstractions;
using Modules.Order.Application.Commands;
using Modules.Order.Application.Contracts;
using Modules.Order.Application.DTOs.Request;
using Modules.Order.Domain;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Order.Property.Tests;

/// <summary>
/// Property 10: For any command execution (Order), if the handler succeeds then
/// PutMetricAsync must be called with the success metric name; if the handler throws
/// then PutMetricAsync must be called with the failure metric name.
/// Never emits the wrong metric (success when fail, or failure when success).
/// Validates: Requirements 5.2, 5.3
/// </summary>
public class OrderCloudWatchMetricsTests
{
    // Generators for valid order request data
    private static Gen<ReceiverRequestDto> GenReceiver() =>
        from name in Gen.String[Gen.Char.AlphaNumeric, 1, 30]
        from phone in Gen.String[Gen.Char.AlphaNumeric, 10, 10]
        from address in Gen.String[Gen.Char.AlphaNumeric, 5, 100]
        select new ReceiverRequestDto(name, phone, address);

    private static Gen<CreateOrderItemRequestDto> GenOrderItem() =>
        from productId in Gen.Guid
        from skuId in Gen.Guid
        from qty in Gen.Int[1, 10]
        select new CreateOrderItemRequestDto(productId, skuId, qty);

    private static Gen<CreateOrderCommand> GenSuccessCommand() =>
        from userId in Gen.Guid
        from receiver in GenReceiver()
        from item in GenOrderItem()
        select new CreateOrderCommand(
            userId,
            receiver,
            PaymentMethod.COD,
            null,
            [item]);

    /// <summary>
    /// Sets up a handler where the DB operations succeed.
    /// Returns (handler, cloudWatch mock).
    /// </summary>
    private static (CreateOrderHandler handler, ICloudWatchService cloudWatch) BuildSuccessHandler(CreateOrderCommand command)
    {
        var repo = Substitute.For<IOrderRepository>();
        var db = Substitute.For<IOrderDbContext>();
        var productService = Substitute.For<IProductService>();
        var mapper = Substitute.For<IMapper>();
        var cloudWatch = Substitute.For<ICloudWatchService>();

        // Product lookup succeeds for any product id
        productService
            .GetProductAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(ci => new ProductDto
            {
                Id = ci.Arg<Guid>(),
                Name = "Test Product",
                Price = 100m
            });

        // Code uniqueness check always returns false (code is unique)
        repo.CodeExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);

        repo.CreateAsync(Arg.Any<Modules.Order.Domain.Order>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        db.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(1);

        cloudWatch.PutMetricAsync(Arg.Any<string>(), Arg.Any<double>(), Arg.Any<string>(), Arg.Any<Dictionary<string, string>>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var handler = new CreateOrderHandler(repo, db, productService, mapper, cloudWatch, NullLogger<CreateOrderHandler>.Instance);
        return (handler, cloudWatch);
    }

    /// <summary>
    /// Sets up a handler where SaveChangesAsync throws.
    /// Returns (handler, cloudWatch mock).
    /// </summary>
    private static (CreateOrderHandler handler, ICloudWatchService cloudWatch) BuildFailureHandler(CreateOrderCommand command)
    {
        var repo = Substitute.For<IOrderRepository>();
        var db = Substitute.For<IOrderDbContext>();
        var productService = Substitute.For<IProductService>();
        var mapper = Substitute.For<IMapper>();
        var cloudWatch = Substitute.For<ICloudWatchService>();

        productService
            .GetProductAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(ci => new ProductDto
            {
                Id = ci.Arg<Guid>(),
                Name = "Test Product",
                Price = 100m
            });

        repo.CodeExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);

        repo.CreateAsync(Arg.Any<Modules.Order.Domain.Order>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Simulate a failure during save
        db.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Throws(new Exception("Simulated DB failure"));

        cloudWatch.PutMetricAsync(Arg.Any<string>(), Arg.Any<double>(), Arg.Any<string>(), Arg.Any<Dictionary<string, string>>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var handler = new CreateOrderHandler(repo, db, productService, mapper, cloudWatch, NullLogger<CreateOrderHandler>.Instance);
        return (handler, cloudWatch);
    }

    private static void WithXRaySegment(Action action)
    {
        AWSXRayRecorder.Instance.BeginSegment("test");
        try { action(); }
        finally { AWSXRayRecorder.Instance.EndSegment(); }
    }

    /// <summary>
    /// Property 10 (success path): When the handler succeeds, "order.created" metric is emitted
    /// and "order.failed" is NOT emitted.
    /// </summary>
    [Fact]
    public void Property10_Success_EmitsOrderCreated_NotOrderFailed()
    {
        GenSuccessCommand().Sample(command =>
        {
            var (handler, cloudWatch) = BuildSuccessHandler(command);

            WithXRaySegment(() =>
            {
                handler.Handle(command, CancellationToken.None).GetAwaiter().GetResult();
            });

            cloudWatch.Received().PutMetricAsync("order.created", 1, "Count", Arg.Any<Dictionary<string, string>>(), Arg.Any<CancellationToken>());
            cloudWatch.DidNotReceive().PutMetricAsync("order.failed", Arg.Any<double>(), Arg.Any<string>(), Arg.Any<Dictionary<string, string>>(), Arg.Any<CancellationToken>());
        });
    }

    /// <summary>
    /// Property 10 (failure path): When the handler throws, "order.failed" metric is emitted
    /// and "order.created" is NOT emitted.
    /// </summary>
    [Fact]
    public void Property10_Failure_EmitsOrderFailed_NotOrderCreated()
    {
        GenSuccessCommand().Sample(command =>
        {
            var (handler, cloudWatch) = BuildFailureHandler(command);

            WithXRaySegment(() =>
            {
                Assert.ThrowsAny<Exception>(() =>
                    handler.Handle(command, CancellationToken.None).GetAwaiter().GetResult());
            });

            cloudWatch.Received().PutMetricAsync("order.failed", 1, "Count", Arg.Any<Dictionary<string, string>>(), Arg.Any<CancellationToken>());
            cloudWatch.DidNotReceive().PutMetricAsync("order.created", Arg.Any<double>(), Arg.Any<string>(), Arg.Any<Dictionary<string, string>>(), Arg.Any<CancellationToken>());
        });
    }

    /// <summary>
    /// Property 10 (latency): When the handler succeeds, "order.latency_ms" metric is emitted
    /// with a non-negative value.
    /// </summary>
    [Fact]
    public void Property10_Success_EmitsLatencyMetric_NonNegative()
    {
        GenSuccessCommand().Sample(command =>
        {
            var (handler, cloudWatch) = BuildSuccessHandler(command);
            double? capturedLatency = null;

            cloudWatch
                .When(cw => cw.PutMetricAsync("order.latency_ms", Arg.Any<double>(), Arg.Any<string>(), Arg.Any<Dictionary<string, string>>(), Arg.Any<CancellationToken>()))
                .Do(ci => capturedLatency = ci.ArgAt<double>(1));

            WithXRaySegment(() =>
            {
                handler.Handle(command, CancellationToken.None).GetAwaiter().GetResult();
            });

            Assert.NotNull(capturedLatency);
            Assert.True(capturedLatency >= 0, $"Latency must be non-negative, got {capturedLatency}");
        });
    }
}
