using System.Globalization;
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
using Modules.Product.Application.Contracts;
using NSubstitute;
using Xunit;

namespace Tests.SystemFlowAudit;

public class PreservationPropertyTests
{
    private static (PaymentController controller, IMediator mediator, ICloudWatchService cloudWatch) BuildController(string? sepayKey = null)
    {
        var mediator = NSubstitute.Substitute.For<IMediator>();
        var cloudWatch = NSubstitute.Substitute.For<ICloudWatchService>();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new System.Collections.Generic.Dictionary<string, string?>
            {
                ["Payment:SePay:SecretKey"] = sepayKey,
                ["Payment:Payout:ReleaseDelayDays"] = "3"
            })
            .Build();

        cloudWatch.PutMetricAsync(Arg.Any<string>(), Arg.Any<double>(), Arg.Any<string>(), Arg.Any<System.Collections.Generic.Dictionary<string, string>>(), Arg.Any<System.Threading.CancellationToken>()).Returns(System.Threading.Tasks.Task.CompletedTask);

        var controller = new PaymentController(
            mediator,
            cloudWatch,
            config,
            NullLogger<PaymentController>.Instance);

        return (controller, mediator, cloudWatch);
    }

    private static string CreateSePaySignature(string secretKey, string timestamp, string body)
    {
        var timestampBytes = Encoding.UTF8.GetBytes(timestamp);
        var bodyBytes = Encoding.UTF8.GetBytes(body);
        var signedPayloadBytes = new byte[timestampBytes.Length + 1 + bodyBytes.Length];

        Buffer.BlockCopy(timestampBytes, 0, signedPayloadBytes, 0, timestampBytes.Length);
        signedPayloadBytes[timestampBytes.Length] = (byte)'.';
        Buffer.BlockCopy(bodyBytes, 0, signedPayloadBytes, timestampBytes.Length + 1, bodyBytes.Length);

        var hashBytes = HMACSHA256.HashData(Encoding.UTF8.GetBytes(secretKey), signedPayloadBytes);
        return "sha256=" + Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    private static DefaultHttpContext BuildHttpContext(string body, string signature, string timestamp)
    {
        var ctx = new DefaultHttpContext();
        var bodyBytes = Encoding.UTF8.GetBytes(body);
        ctx.Request.Body = new System.IO.MemoryStream(bodyBytes);
        ctx.Request.ContentLength = bodyBytes.Length;
        ctx.Request.Headers["X-SePay-Signature"] = signature;
        ctx.Request.Headers["X-SePay-Timestamp"] = timestamp;
        return ctx;
    }

    [Fact]
    public async System.Threading.Tasks.Task SePayWebhook_InvalidSignature_IsRejected()
    {
        var key = "test-key";
        var (controller, _, cloudWatch) = BuildController(key);

        var payload = JsonSerializer.Serialize(new { payment_code = "PAY-001", transaction_status = "success", transaction_id = "TXN-1", amount = 100 });
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);
        controller.ControllerContext = new ControllerContext { HttpContext = BuildHttpContext(payload, "bad-signature", timestamp) };

        var result = await controller.SePayWebhook(System.Threading.CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
        await cloudWatch.Received().PutMetricAsync("payment.webhook.failed", 1, "Count", Arg.Any<System.Collections.Generic.Dictionary<string, string>>(), Arg.Any<System.Threading.CancellationToken>());
    }

    [Fact]
    public async System.Threading.Tasks.Task SePayWebhook_ValidSignature_IsAccepted()
    {
        var key = "test-key";
        var (controller, mediator, cloudWatch) = BuildController(key);

        mediator
            .Send(Arg.Any<Modules.Payment.Application.Commands.ProcessSePayWebhookCommand>(), Arg.Any<System.Threading.CancellationToken>())
            .Returns(System.Threading.Tasks.Task.FromResult(new Modules.Payment.Application.Commands.ProcessSePayWebhookResult(true)));

        var payload = JsonSerializer.Serialize(new { payment_code = "PAY-001", transaction_status = "success", transaction_id = "TXN-1", amount = 100 });
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);
        var signature = CreateSePaySignature(key, timestamp, payload);
        controller.ControllerContext = new ControllerContext { HttpContext = BuildHttpContext(payload, signature, timestamp) };

        var result = await controller.SePayWebhook(System.Threading.CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
        await cloudWatch.DidNotReceive().PutMetricAsync("payment.webhook.failed", 1, "Count", Arg.Any<System.Collections.Generic.Dictionary<string, string>>(), Arg.Any<System.Threading.CancellationToken>());
    }

    [Fact]
    public async System.Threading.Tasks.Task InitiatePayment_InvalidAmount_ReturnsBadRequest()
    {
        var (controller, mediator, _) = BuildController();
        mediator
            .Send(Arg.Any<Modules.Payment.Application.Commands.InitiatePaymentCommand>(), Arg.Any<System.Threading.CancellationToken>())
            .Returns(System.Threading.Tasks.Task.FromResult(new Modules.Payment.Application.Commands.InitiatePaymentResponseDto(Guid.NewGuid(), "PAY-001", Modules.Payment.Domain.PaymentStatus.Pending, null)));

        var dto = new Modules.Payment.Application.DTOs.Request.InitiatePaymentRequestDto(
            Guid.NewGuid(),
            Guid.NewGuid(),
            0,
            Modules.Payment.Domain.PaymentMethod.COD,
            new System.Collections.Generic.List<Modules.Payment.Application.DTOs.Request.OrderItemRequestDto>());

        controller.ControllerContext = new ControllerContext();

        var result = await controller.InitiatePayment(dto, System.Threading.CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Contains("greater than 0", badRequest.Value?.ToString() ?? string.Empty);
    }
}
