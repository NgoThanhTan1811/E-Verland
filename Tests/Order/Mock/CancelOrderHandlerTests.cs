using Infra.AWS.CloudWatch;
using Modules.Order.Application.Commands;
using Modules.Order.Application.Contracts;
using Modules.Order.Domain;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Tests.Order.Mock;

public class CancelOrderHandlerTests
{
    private static (CancelOrderHandler handler, IOrderRepository repo, IOrderDbContext db, ICloudWatchService cloudWatch)
        BuildHandler()
    {
        var repo = Substitute.For<IOrderRepository>();
        var db = Substitute.For<IOrderDbContext>();
        var cloudWatch = Substitute.For<ICloudWatchService>();

        db.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
        cloudWatch.PutMetricAsync(Arg.Any<string>(), Arg.Any<double>(), Arg.Any<string>(),
            Arg.Any<Dictionary<string, string>>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        var handler = new CancelOrderHandler(repo, db, cloudWatch);
        return (handler, repo, db, cloudWatch);
    }

    private static Modules.Order.Domain.Order PendingOrder(Guid userId) => new()
    {
        UserId = userId,
        Code = "ORD-01012025-1234", 
        Status = OrderStatus.Pending,
        Receiver = ReceiverSnapshot.Create("Alice", "0123456789", "789 Oak Ave"),
        Items = []
    };

    [Fact]
    public async Task Handle_ValidCancel_EmitsOrderCancelledMetric()
    {
        var (handler, repo, _, cloudWatch) = BuildHandler();
        var userId = Guid.NewGuid();
        var order = PendingOrder(userId);

        repo.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        repo.UpdateAsync(Arg.Any<Modules.Order.Domain.Order>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        await handler.Handle(new CancelOrderCommand(order.Id, userId), CancellationToken.None);

        await cloudWatch.Received().PutMetricAsync("order.cancelled", 1, "Count");
    }

    [Fact]
    public async Task Handle_OrderNotFound_ThrowsKeyNotFoundException()
    {
        var (handler, repo, _, _) = BuildHandler();
        repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Modules.Order.Domain.Order?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            handler.Handle(new CancelOrderCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_AlreadyCancelled_ThrowsInvalidOperationException()
    {
        var (handler, repo, _, _) = BuildHandler();
        var userId = Guid.NewGuid();
        var order = PendingOrder(userId);
        order.Status = OrderStatus.Canceled;

        repo.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new CancelOrderCommand(order.Id, userId), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WrongUser_ThrowsUnauthorizedAccessException()
    {
        var (handler, repo, _, _) = BuildHandler();
        var ownerId = Guid.NewGuid();
        var otherId = Guid.NewGuid();
        var order = PendingOrder(ownerId);

        repo.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            handler.Handle(new CancelOrderCommand(order.Id, otherId), CancellationToken.None));
    }
}
