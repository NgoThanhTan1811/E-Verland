using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Infra.AWS.CloudWatch;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Modules.Payment.Application.Contracts;
using Modules.Payment.Application.Commands;
using Modules.Payment.Application.DTOs.Request;
using Modules.Payment.Application.DTOs.Response;
using Modules.Payment.Application.Queries;
using Modules.Payment.Domain;
using Modules.Payment.Infrastructure.Services;
using Modules.Product.Application.Contracts;

namespace Modules.Payment.Api.Controllers;

[ApiController]
[EnableRateLimiting("payment")]
[Route("api/[controller]")]
public class PaymentController(
    IMediator mediator,
    IProductReservationService reservationService,
    ICloudWatchService cloudWatch,
    IConfiguration configuration,
    IWebhookIdempotencyService webhookIdempotency,
    ILedgerService ledgerService,
    ISellerBalanceService sellerBalanceService) : ControllerBase
{
    private readonly IMediator _mediator = mediator;
    private readonly IProductReservationService _reservationService = reservationService;
    private readonly ICloudWatchService _cloudWatch = cloudWatch;
    private readonly IConfiguration _configuration = configuration;
    private readonly IWebhookIdempotencyService _webhookIdempotency = webhookIdempotency;
    private readonly ILedgerService _ledgerService = ledgerService;
    private readonly ISellerBalanceService _sellerBalanceService = sellerBalanceService;

    [Authorize]
    [HttpPost]
    [ProducesResponseType(typeof(InitiatePaymentResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<InitiatePaymentResponseDto>> InitiatePayment(
        [FromBody] InitiatePaymentRequestDto dto,
        CancellationToken ct)
    {
        try
        {
            var command = new InitiatePaymentCommand(
                dto.OrderId,
                dto.UserId,
                dto.Amount,
                dto.Method,
                dto.Items.Select(i => new OrderItemDto(i.SkuId, i.Quantity)).ToList()
            );

            var result = await _mediator.Send(command, ct);
            return CreatedAtAction(nameof(GetPaymentById), new { id = result.Id }, result);
        }
        catch (SePayApiException)
        {
            return StatusCode(StatusCodes.Status502BadGateway, new
            {
                message = "Payment provider is temporarily unavailable. Please try again later."
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize]
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(PaymentResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PaymentResponseDto>> GetPaymentById(Guid id, CancellationToken ct)
    {
        try
        {
            var query = new GetPaymentByIdQuery(id);
            var result = await _mediator.Send(query, ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [Authorize]
    [HttpGet("payment-order/{orderId}")]
    [ProducesResponseType(typeof(PaymentResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PaymentResponseDto>> GetPaymentByOrderId(Guid orderId, CancellationToken ct)
    {
        var query = new GetPaymentByOrderIdQuery(orderId);
        var result = await _mediator.Send(query, ct);

        if (result == null)
            return NotFound(new { message = "Payment not found for this order" });

        return Ok(result);
    }

    [Authorize]
    [HttpGet("payment-code/{code}")]
    [ProducesResponseType(typeof(PaymentResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PaymentResponseDto>> GetPaymentByCode(string code, CancellationToken ct)
    {
        var query = new GetPaymentByCodeQuery(code);
        var result = await _mediator.Send(query, ct);

        if (result == null)
            return NotFound(new { message = "Payment not found" });

        return Ok(result);
    }

    [Authorize]
    [HttpGet("payment-user/{userId}")]
    [ProducesResponseType(typeof(List<PaymentOverviewResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<PaymentOverviewResponseDto>>> GetUserPayments(
        Guid userId,
        CancellationToken ct)
    {
        var query = new GetUserPaymentsQuery(userId);
        var result = await _mediator.Send(query, ct);
        return Ok(result);
    }

    [Authorize(Policy = "AdminPolicy")]
    [HttpPatch("payment:{id}/status")]
    [ProducesResponseType(typeof(PaymentResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PaymentResponseDto>> UpdatePaymentStatus(
        Guid id,
        [FromBody] UpdatePaymentStatusRequestDto dto,
        CancellationToken ct)
    {
        try
        {
            var command = new UpdatePaymentStatusCommand(id, dto.Status);
            var result = await _mediator.Send(command, ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [AllowAnonymous]
    [HttpPost("webhook/sepay")]
    [DisableRequestSizeLimit]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SePayWebhook(CancellationToken ct)
    {
        // Emit metric at the very start, before any validation
        await _cloudWatch.PutMetricAsync("payment.webhook.received", 1, "Count", ct: ct);

        // Read raw body for HMAC verification
        Request.EnableBuffering();
        using var reader = new StreamReader(Request.Body, Encoding.UTF8, leaveOpen: true);
        var rawBody = await reader.ReadToEndAsync(ct);
        Request.Body.Position = 0;

        // Verify HMAC-SHA256 signature
        var sepayKey = _configuration["Payment:SePay:SecretKey"]
            ?? Environment.GetEnvironmentVariable("SEPAY_SECRET_KEY")
            ?? Environment.GetEnvironmentVariable("SEPAY_KEY")
            ?? string.Empty;

        if (string.IsNullOrWhiteSpace(sepayKey))
        {
            await _cloudWatch.PutMetricAsync("payment.webhook.failed", 1, "Count", ct: ct);
            return BadRequest(new { message = "SePay signature key is not configured" });
        }

        var keyBytes = Encoding.UTF8.GetBytes(sepayKey);
        var bodyBytes = Encoding.UTF8.GetBytes(rawBody);
        var computedHash = HMACSHA256.HashData(keyBytes, bodyBytes);
        var computedSignature = Convert.ToHexString(computedHash).ToLowerInvariant();

        var receivedSignature = Request.Headers["X-SePay-Signature"].ToString().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(receivedSignature))
        {
            await _cloudWatch.PutMetricAsync("payment.webhook.failed", 1, "Count", ct: ct);
            return BadRequest(new { message = "Missing X-SePay-Signature" });
        }

        var signatureValid = CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(computedSignature),
            Encoding.UTF8.GetBytes(receivedSignature));

        if (!signatureValid)
        {
            await _cloudWatch.PutMetricAsync("payment.webhook.failed", 1, "Count", ct: ct);
            return BadRequest(new { message = "Invalid signature" });
        }

        // Deserialize payload
        SePayWebhookDto? payload;
        try
        {
            payload = JsonSerializer.Deserialize<SePayWebhookDto>(rawBody);
        }
        catch
        {
            return BadRequest(new { message = "Invalid payload" });
        }

        if (payload is null || string.IsNullOrEmpty(payload.PaymentCode))
            return BadRequest(new { message = "Missing payment_code" });

        var idempotencyKey = !string.IsNullOrWhiteSpace(payload.WebhookId)
            ? payload.WebhookId
            : payload.PaymentCode;

        if (await _webhookIdempotency.IsProcessedAsync(idempotencyKey, ct))
        {
            return Ok(new { success = true });
        }

        // Look up payment by code
        var payment = await _mediator.Send(new GetPaymentByCodeQuery(payload.PaymentCode), ct);
        if (payment is null)
        {
            await _cloudWatch.PutMetricAsync("payment.webhook.failed", 1, "Count", ct: ct);
            return NotFound(new { message = "Payment not found" });
        }

        if (payment.Status == PaymentStatus.Success)
        {
            await _webhookIdempotency.TryMarkAsProcessedAsync(
                idempotencyKey,
                payload.PaymentCode,
                payload.TransactionStatus,
                ct);
            return Ok(new { success = true });
        }

        // Update status and call reservation service
        if (payload.TransactionStatus == "success")
        {
            await _mediator.Send(new UpdatePaymentStatusCommand(payment.Id, PaymentStatus.Success), ct);
            await _reservationService.ConfirmReservationAsync(payment.Id, ct);

            await _ledgerService.RecordIncomingPaymentAsync(
                payment.OrderId,
                payment.Amount,
                "VND",
                $"incoming:{idempotencyKey}",
                "sepay-webhook",
                ct);

            var sellerId = payload.SellerId ?? Guid.Empty;
            var releaseDelayDays = int.TryParse(
                _configuration["Payment:Payout:ReleaseDelayDays"],
                out var configuredDelayDays)
                ? Math.Max(1, configuredDelayDays)
                : 3;
            await _sellerBalanceService.EnsurePendingBalanceAsync(
                payment.OrderId,
                sellerId,
                payment.Amount,
                "VND",
                DateTime.UtcNow.AddDays(releaseDelayDays),
                ct);
        }
        else if (payload.TransactionStatus == "failed")
        {
            await _mediator.Send(new UpdatePaymentStatusCommand(payment.Id, PaymentStatus.Failed), ct);
            await _reservationService.ReleaseReservationAsync(payment.Id, ct);
        }

        await _webhookIdempotency.TryMarkAsProcessedAsync(
            idempotencyKey,
            payload.PaymentCode,
            payload.TransactionStatus,
            ct);

        return Ok(new { success = true });
    }
}

public sealed record SePayWebhookDto(
    [property: JsonPropertyName("webhook_id")] string? WebhookId,
    [property: JsonPropertyName("payment_code")] string PaymentCode,
    [property: JsonPropertyName("transaction_status")] string TransactionStatus,
    [property: JsonPropertyName("transaction_id")] string TransactionId,
    [property: JsonPropertyName("amount")] decimal Amount,
    [property: JsonPropertyName("seller_id")] Guid? SellerId
);
