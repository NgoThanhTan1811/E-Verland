using Amazon.XRay.Recorder.Core;
using Infra.AWS.CloudWatch;
using Microsoft.Extensions.Logging.Abstractions;
using Modules.Order.Application.Commands;
using Modules.Order.Application.Contracts;
using Modules.Order.Application.DTOs.Request;
using Modules.Order.Application.DTOs.Response;
using Modules.Order.Domain;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Tests.Order.Unit;

public class CreateOrderHandlerTests
{
    private static (CreateOrderHandler handler, IOrderRepository repo, IOrderDbContext db,
        IProductService productService, ICloudWatchService cloudWatch) BuildHandler()
    {
        var repo = Substitute.For<IOrderRepository>();
        var db = Substitute.For<IOrderDbContext>();
        var productService = Substitute.For<IProductService>();
        var cloudWatch = Substitute.For<ICloudWatchService>();

        repo.CodeExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        repo.CreateAsync(Arg.Any<Modules.Order.Domain.Order>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        db.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
        cloudWatch.PutMetricAsync(Arg.Any<string>(), Arg.Any<double>(), Arg.Any<string>(),
            Arg.Any<Dictionary<string, string>>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        var handler = new CreateOrderHandler(repo, db, productService, cloudWatch,
            NullLogger<CreateOrderHandler>.Instance);
        return (handler, repo, db, productService, cloudWatch);
    }

    private static ShippingAddressRequestDto ValidAddress() =>
        new("123 Main St", 1442, "W001", "Ward", "District", "Province");

    private static CreateOrderCommand ValidCommand(Guid? userId = null) =>
        new(
            userId ?? Guid.NewGuid(),
            ValidAddress(),
            new ReceiverRequestDto("John Doe", "0123456789"),
            1000,
            20,
            15,
            10,
            null,
            null,
            null,
            null,
            null,
            PaymentMethod.COD,
            null,
            [new CreateOrderItemRequestDto(Guid.NewGuid(), Guid.NewGuid(), 2)]
        );

    private static void WithXRay(Action action)
    {
        AWSXRayRecorder.Instance.BeginSegment("test");
        try { action(); }
        finally { AWSXRayRecorder.Instance.EndSegment(); }
    }

    [Fact]
    public async Task Handle_WithValidItems_ReturnsOrderId()
    {
        var (handler, _, _, productService, _) = BuildHandler();
        var command = ValidCommand();

        productService.GetProductAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new ProductDto { Id = command.Items[0].ProductId, Name = "Widget", Price = 50m });

        CreateOrderResponseDto result = null!;
        WithXRay(() =>
        {
            result = handler.Handle(command, CancellationToken.None).GetAwaiter().GetResult();
        });

        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.False(string.IsNullOrEmpty(result.Code));
    }

    [Fact]
    public async Task Handle_WithEmptyItems_ThrowsArgumentException()
    {
        var (handler, _, _, _, _) = BuildHandler();
        var command = new CreateOrderCommand(
            Guid.NewGuid(),
            new ShippingAddressRequestDto("456 Elm St", 1442, "W002", "Ward", "District", "Province"),
            new ReceiverRequestDto("Jane", "0987654321"),
            1000,
            20,
            15,
            10,
            null,
            null,
            null,
            null,
            null,
            PaymentMethod.COD,
            null,
            []
        );

        await Assert.ThrowsAsync<ArgumentException>(() =>
            handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenProductNotFound_ThrowsArgumentException()
    {
        var (handler, _, _, productService, _) = BuildHandler();
        var command = ValidCommand();

        productService.GetProductAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((ProductDto?)null);

        await Assert.ThrowsAsync<ArgumentException>(() =>
        {
            Task result = null!;
            WithXRay(() =>
            {
                result = handler.Handle(command, CancellationToken.None);
            });
            return result;
        });
    }

    [Fact]
    public void Handle_WhenDbFails_EmitsOrderFailedMetric()
    {
        var (handler, _, db, productService, cloudWatch) = BuildHandler();
        var command = ValidCommand();

        productService.GetProductAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new ProductDto { Id = command.Items[0].ProductId, Name = "Widget", Price = 50m });

        db.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Throws(new Exception("DB failure"));

        WithXRay(() =>
        {
            Assert.ThrowsAny<Exception>(() =>
                handler.Handle(command, CancellationToken.None).GetAwaiter().GetResult());
        });

        cloudWatch.Received().PutMetricAsync("order.failed", 1, "Count",
            Arg.Any<Dictionary<string, string>>(), Arg.Any<CancellationToken>());
    }
}
