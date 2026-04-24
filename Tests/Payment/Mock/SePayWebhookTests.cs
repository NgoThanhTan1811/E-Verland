using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Infra.AWS.CloudWatch;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Modules.Order.Application.Contracts;
using Modules.Payment.Api.Controllers;
using Modules.Payment.Application.Contracts;
using Modules.Payment.Application.DTOs.Response;
using Modules.Payment.Application.Queries;
using Modules.Payment.Domain;
using Modules.Product.Application.Contracts;
using NSubstitute;
using Xunit;

namespace Tests.Payment.Mock;

public class SePayWebhookTests
{
    private static (PaymentController controller, IMediator mediator, ICloudWatchService cloudWatch)
        BuildController(string? sepayKey = null)
    {
        var mediator = Substitute.For<IMediator>();
        var reservationService = Substitute.For<IProductReservationService>();
        var cloudWatch = Substitute.For<ICloudWatchService>();
        var webhookIdempotency = Substitute.For<IWebhookIdempotencyService>();
        var ledgerService = Substitute.For<ILedgerService>();
        var sellerBalanceService = Substitute.For<ISellerBalanceService>();
        var orderRepository = Substitute.For<IOrderRepository>();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Payment:SePay:SecretKey"] = sepayKey,
                ["Payment:Payout:ReleaseDelayDays"] = "3"
            })
            .Build();

        cloudWatch.PutMetricAsync(Arg.Any<string>(), Arg.Any<double>(), Arg.Any<string>(),
            Arg.Any<Dictionary<string, string>>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        webhookIdempotency.IsProcessedAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        webhookIdempotency.TryMarkAsProcessedAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var controller = new PaymentController(
            mediator,
            reservationService,
            cloudWatch,
            config,
            NullLogger<PaymentController>.Instance,
            webhookIdempotency,
            ledgerService,
            sellerBalanceService,
            orderRepository);
        return (controller, mediator, cloudWatch);
    }

    private static DefaultHttpContext BuildHttpContext(string body, string signature)
    {
        var ctx = new DefaultHttpContext();
        var bodyBytes = Encoding.UTF8.GetBytes(body);
        ctx.Request.Body = new MemoryStream(bodyBytes);
        ctx.Request.ContentLength = bodyBytes.Length;
        ctx.Request.Headers["X-SePay-Signature"] = signature;
        return ctx;
    }

    private static string ComputeSignature(string key, string body)
    {
        var keyBytes = Encoding.UTF8.GetBytes(key);
        var bodyBytes = Encoding.UTF8.GetBytes(body);
        var hash = HMACSHA256.HashData(keyBytes, bodyBytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    [Fact]
    public async Task SePayWebhook_Always_EmitsWebhookReceivedMetric()
    {
        var key = "test-key";
        var (controller, mediator, cloudWatch) = BuildController(key);

        var payload = JsonSerializer.Serialize(new { payment_code = "PAY-001", transaction_status = "success", transaction_id = "TXN-1", amount = 100 });
        var sig = ComputeSignature(key, payload);
        controller.ControllerContext = new ControllerContext { HttpContext = BuildHttpContext(payload, sig) };

        var payment = new PaymentResponseDto(Guid.NewGuid(), "PAY-001", Guid.NewGuid(), Guid.NewGuid(),
            100m, PaymentMethod.COD, PaymentStatus.Pending, DateTime.UtcNow, null);
        mediator.Send(Arg.Any<GetPaymentByCodeQuery>(), Arg.Any<CancellationToken>()).Returns(payment);
        mediator.Send(Arg.Any<IRequest<PaymentResponseDto>>(), Arg.Any<CancellationToken>()).Returns(payment);

        await controller.SePayWebhook(CancellationToken.None);

        await cloudWatch.Received().PutMetricAsync("payment.webhook.received", 1, "Count",
            Arg.Any<Dictionary<string, string>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SePayWebhook_InvalidSignature_EmitsWebhookFailedMetric()
    {
        var key = "test-key";
        var (controller, _, cloudWatch) = BuildController(key);

        var payload = JsonSerializer.Serialize(new { payment_code = "PAY-001", transaction_status = "success", transaction_id = "TXN-1", amount = 100 });
        controller.ControllerContext = new ControllerContext { HttpContext = BuildHttpContext(payload, "bad-signature") };

        var result = await controller.SePayWebhook(CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
        await cloudWatch.Received().PutMetricAsync("payment.webhook.failed", 1, "Count",
            Arg.Any<Dictionary<string, string>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SePayWebhook_PaymentNotFound_EmitsWebhookFailedMetric()
    {
        var key = "test-key";
        var (controller, mediator, cloudWatch) = BuildController(key);

        var payload = JsonSerializer.Serialize(new { payment_code = "PAY-NOTFOUND", transaction_status = "success", transaction_id = "TXN-2", amount = 50 });
        var sig = ComputeSignature(key, payload);
        controller.ControllerContext = new ControllerContext { HttpContext = BuildHttpContext(payload, sig) };

        mediator.Send(Arg.Any<GetPaymentByCodeQuery>(), Arg.Any<CancellationToken>())
            .Returns((PaymentResponseDto?)null);

        var result = await controller.SePayWebhook(CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
        await cloudWatch.Received().PutMetricAsync("payment.webhook.failed", 1, "Count",
            Arg.Any<Dictionary<string, string>>(), Arg.Any<CancellationToken>());
    }
}
