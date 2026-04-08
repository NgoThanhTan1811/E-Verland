using Amazon.XRay.Recorder.Core;
using Infra.AWS.CloudWatch;
using Microsoft.Extensions.Logging.Abstractions;
using Modules.Payment.Application.Commands;
using Modules.Payment.Application.Contracts;
using Modules.Payment.Domain;
using Modules.Product.Application.Contracts;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Tests.Payment.Unit;

public class InitiatePaymentHandlerTests
{
    private static (InitiatePaymentHandler handler, IPaymentRepository repo, IPaymentDbContext db,
        IProductReservationService reservation, ISePayClient sePayClient, ICloudWatchService cloudWatch)
        BuildHandler()
    {
        var repo = Substitute.For<IPaymentRepository>();
        var db = Substitute.For<IPaymentDbContext>();
        var reservation = Substitute.For<IProductReservationService>();
        var sePayClient = Substitute.For<ISePayClient>();
        var cloudWatch = Substitute.For<ICloudWatchService>();

        repo.GetByOrderIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Modules.Payment.Domain.Payment?)null);
        repo.CreateAsync(Arg.Any<Modules.Payment.Domain.Payment>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        db.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
        reservation.ReserveStockAsync(Arg.Any<Guid>(), Arg.Any<IEnumerable<(Guid, int)>>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        cloudWatch.PutMetricAsync(Arg.Any<string>(), Arg.Any<double>(), Arg.Any<string>(),
            Arg.Any<Dictionary<string, string>>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        var handler = new InitiatePaymentHandler(repo, db, reservation, sePayClient, cloudWatch,
            NullLogger<InitiatePaymentHandler>.Instance);
        return (handler, repo, db, reservation, sePayClient, cloudWatch);
    }

    private static InitiatePaymentCommand ValidCommand() =>
        new(Guid.NewGuid(), Guid.NewGuid(), 100m, PaymentMethod.COD,
            [new OrderItemDto(Guid.NewGuid(), 1)]);

    private static void WithXRay(Action action)
    {
        AWSXRayRecorder.Instance.BeginSegment("test");
        try { action(); }
        finally { AWSXRayRecorder.Instance.EndSegment(); }
    }

    [Fact]
    public void Handle_NewPayment_ReturnsPaymentDto()
    {
        var (handler, _, _, _, _, _) = BuildHandler();
        var command = ValidCommand();

        InitiatePaymentResponseDto result = null!;
        WithXRay(() =>
        {
            result = handler.Handle(command, CancellationToken.None).GetAwaiter().GetResult();
        });

        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.False(string.IsNullOrEmpty(result.Code));
        Assert.Equal(PaymentStatus.Pending, result.Status);
    }

    [Fact]
    public void Handle_DuplicatePayment_ThrowsInvalidOperationException()
    {
        var (handler, repo, _, _, _, _) = BuildHandler();
        var command = ValidCommand();
        var existing = new Modules.Payment.Domain.Payment { Code = "PAY-001", OrderId = command.OrderId };

        repo.GetByOrderIdAsync(command.OrderId, Arg.Any<CancellationToken>()).Returns(existing);

        WithXRay(() =>
        {
            Assert.Throws<InvalidOperationException>(() =>
                handler.Handle(command, CancellationToken.None).GetAwaiter().GetResult());
        });
    }

    [Fact]
    public void Handle_Success_EmitsPaymentInitiatedMetric()
    {
        var (handler, _, _, _, _, cloudWatch) = BuildHandler();
        var command = ValidCommand();

        WithXRay(() =>
        {
            handler.Handle(command, CancellationToken.None).GetAwaiter().GetResult();
        });

        cloudWatch.Received().PutMetricAsync("payment.initiated", 1, "Count",
            Arg.Any<Dictionary<string, string>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Handle_Success_EmitsLatencyMetric()
    {
        var (handler, _, _, _, _, cloudWatch) = BuildHandler();
        var command = ValidCommand();
        double? capturedLatency = null;

        cloudWatch.When(cw => cw.PutMetricAsync("payment.latency_ms", Arg.Any<double>(), Arg.Any<string>(),
                Arg.Any<Dictionary<string, string>>(), Arg.Any<CancellationToken>()))
            .Do(ci => capturedLatency = ci.ArgAt<double>(1));

        WithXRay(() =>
        {
            handler.Handle(command, CancellationToken.None).GetAwaiter().GetResult();
        });

        Assert.NotNull(capturedLatency);
        Assert.True(capturedLatency >= 0, $"Latency must be non-negative, got {capturedLatency}");
    }
}
