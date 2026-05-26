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

    private static DefaultHttpContext BuildHttpContext(string body, string signature)
    {
        var ctx = new DefaultHttpContext();
        var bodyBytes = Encoding.UTF8.GetBytes(body);
        ctx.Request.Body = new System.IO.MemoryStream(bodyBytes);
        ctx.Request.ContentLength = bodyBytes.Length;
        ctx.Request.Headers["X-SePay-Signature"] = signature;
        return ctx;
    }

    [Fact]
    public async System.Threading.Tasks.Task SePayWebhook_InvalidSignature_IsRejected()
    {
        var key = "test-key";
        var (controller, _, cloudWatch) = BuildController(key);

        var payload = JsonSerializer.Serialize(new { payment_code = "PAY-001", transaction_status = "success", transaction_id = "TXN-1", amount = 100 });
        controller.ControllerContext = new ControllerContext { HttpContext = BuildHttpContext(payload, "bad-signature") };

        var result = await controller.SePayWebhook(System.Threading.CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
        await cloudWatch.Received().PutMetricAsync("payment.webhook.failed", 1, "Count", Arg.Any<System.Collections.Generic.Dictionary<string, string>>(), Arg.Any<System.Threading.CancellationToken>());
    }
}
