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
using Microsoft.Extensions.Logging;
using Modules.Payment.Application.Commands;
using Modules.Payment.Application.DTOs.Request;
using Modules.Payment.Application.DTOs.Response;
using Modules.Payment.Application.Queries;
using Modules.Payment.Domain;
using Modules.Payment.Infrastructure.Services;

namespace Modules.Payment.Api.Controllers;

[ApiController]
[EnableRateLimiting("payment")]
[Route("api/[controller]")]
public class PaymentController(
    IMediator mediator,
    Product.Application.Contracts.IProductReservationService reservationService,
    ICloudWatchService cloudWatch,
    IConfiguration configuration,
    ILogger<PaymentController> logger,
    Application.Contracts.IWebhookIdempotencyService webhookIdempotency) : ControllerBase
{
    private readonly IMediator _mediator = mediator;
    private readonly ICloudWatchService _cloudWatch = cloudWatch;
    private readonly IConfiguration _configuration = configuration;
    private readonly ILogger<PaymentController> _logger = logger;

    // ── Commands ─────────────────────────────────────────────────────────────

    [Authorize]
    [HttpPost]
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
                dto.Items.Select(i => new OrderItemDto(i.SkuId, i.Quantity)).ToList());

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

    [Authorize(Policy = "AdminPolicy")]
    [HttpPatch("payment:{id}/status")]
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

    // ── Queries ──────────────────────────────────────────────────────────────

    [Authorize]
    [HttpGet("{id}")]
    public async Task<ActionResult<PaymentResponseDto>> GetPaymentById(Guid id, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new GetPaymentByIdQuery(id), ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [Authorize]
    [HttpGet("payment-order/{orderId}")]
    public async Task<ActionResult<PaymentResponseDto>> GetPaymentByOrderId(Guid orderId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetPaymentByOrderIdQuery(orderId), ct);
        return result is null
            ? NotFound(new { message = "Payment not found for this order" })
            : Ok(result);
    }

    [Authorize]
    [HttpGet("payment-code/{code}")]
    public async Task<ActionResult<PaymentResponseDto>> GetPaymentByCode(string code, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetPaymentByCodeQuery(code), ct);
        return result is null
            ? NotFound(new { message = "Payment not found" })
            : Ok(result);
    }

    [Authorize]
    [HttpGet("payment-user/{userId}")]
    public async Task<ActionResult<List<PaymentOverviewResponseDto>>> GetUserPayments(
        Guid userId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetUserPaymentsQuery(userId), ct);
        return Ok(result);
    }

    // ── Webhook ───────────────────────────────────────────────────────────────
    // Responsibility: verify HMAC signature, parse payload, then delegate to handler.
    // No business logic lives here.

    [AllowAnonymous]
    [HttpPost("webhook/sepay")]
    [DisableRequestSizeLimit]
    public async Task<IActionResult> SePayWebhook(CancellationToken ct)
    {
        await _cloudWatch.PutMetricAsync("payment.webhook.received", 1, "Count", ct: ct);

        // ── Read raw body for HMAC verification ──────────────────────────────
        Request.EnableBuffering();
        using var reader = new StreamReader(Request.Body, Encoding.UTF8, leaveOpen: true);
        var rawBody = await reader.ReadToEndAsync(ct);
        Request.Body.Position = 0;

        // ── Verify HMAC-SHA256 signature ──────────────────────────────────────
        var sepayKey = _configuration["SePay:Key"]
            ?? Environment.GetEnvironmentVariable("SEPAY_SECRET_KEY")
            ?? Environment.GetEnvironmentVariable("SEPAY_KEY")
            ?? string.Empty;
    
        if (string.IsNullOrWhiteSpace(sepayKey))
        {
            await _cloudWatch.PutMetricAsync("payment.webhook.failed", 1, "Count", ct: ct);
            _logger.LogWarning("Rejected SePay webhook: signature key not configured");
            return BadRequest(new { message = "SePay signature key is not configured" });
        }

        var computedSignature = Convert.ToHexString(
            HMACSHA256.HashData(Encoding.UTF8.GetBytes(sepayKey), Encoding.UTF8.GetBytes(rawBody))
        ).ToLowerInvariant();

        var receivedSignature = Request.Headers["X-SePay-Signature"].ToString().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(receivedSignature))
        {
            await _cloudWatch.PutMetricAsync("payment.webhook.failed", 1, "Count", ct: ct);
            _logger.LogWarning("Rejected SePay webhook: missing X-SePay-Signature header");
            return BadRequest(new { message = "Missing X-SePay-Signature" });
        }

        var signatureValid = CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(computedSignature),
            Encoding.UTF8.GetBytes(receivedSignature));

        if (!signatureValid)
        {
            await _cloudWatch.PutMetricAsync("payment.webhook.failed", 1, "Count", ct: ct);
            _logger.LogWarning("Rejected SePay webhook: invalid signature");
            return BadRequest(new { message = "Invalid signature" });
        }

        // ── Parse payload ─────────────────────────────────────────────────────
        SePayWebhookDto? payload;
        try
        {
            payload = JsonSerializer.Deserialize<SePayWebhookDto>(rawBody);
        }
        catch
        {
            await _cloudWatch.PutMetricAsync("payment.webhook.failed", 1, "Count", ct: ct);
            _logger.LogWarning("Rejected SePay webhook: payload could not be parsed");
            return BadRequest(new { message = "Invalid payload" });
        }

        if (payload is null || string.IsNullOrEmpty(payload.PaymentCode))
        {
            await _cloudWatch.PutMetricAsync("payment.webhook.failed", 1, "Count", ct: ct);
            _logger.LogWarning("Rejected SePay webhook: missing payment_code");
            return BadRequest(new { message = "Missing payment_code" });
        }

        var normalizedStatus = NormalizeTransactionStatus(payload.TransactionStatus);
        if (normalizedStatus is null)
        {
            await _cloudWatch.PutMetricAsync("payment.webhook.failed", 1, "Count", ct: ct);
            _logger.LogWarning("Rejected SePay webhook: unsupported transaction_status {Status}",
                payload.TransactionStatus);
            return BadRequest(new
            {
                message = "Unsupported transaction_status. Allowed: success, failed, refunded."
            });
        }

        // ── Dispatch to handler ───────────────────────────────────────────────
        var idempotencyKey = !string.IsNullOrWhiteSpace(payload.WebhookId)
            ? payload.WebhookId
            : payload.PaymentCode;

        try
        {
            var command = new ProcessSePayWebhookCommand(
                IdempotencyKey: idempotencyKey,
                PaymentCode: payload.PaymentCode,
                TransactionStatus: normalizedStatus,
                Amount: payload.Amount,
                SellerId: payload.SellerId);

            var result = await _mediator.Send(command, ct);
            return Ok(new { success = result.Success, compensated = result.Compensated });
        }
        catch (KeyNotFoundException ex)
        {
            await _cloudWatch.PutMetricAsync("payment.webhook.failed", 1, "Count", ct: ct);
            return NotFound(new { message = ex.Message });
        }
        catch (AggregateException ex)
        {
            _logger.LogError(ex, "Webhook processing and compensation both failed for {PaymentCode}",
                payload.PaymentCode);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                code = "PAYMENT_COMPENSATION_FAILED",
                message = "Payment processing failed and compensation also failed.",
                detail = ex.Message
            });
        }
        catch (Exception ex)
        {
            await _cloudWatch.PutMetricAsync("payment.webhook.failed", 1, "Count", ct: ct);
            _logger.LogError(ex, "Webhook processing failed for {PaymentCode}", payload.PaymentCode);
            return BadRequest(new
            {
                code = "PAYMENT_PROCESSING_FAILED",
                message = "Payment processing failed and has been compensated.",
                detail = ex.Message
            });
        }
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private static string? NormalizeTransactionStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status)) return null;
        return status.Trim().ToLowerInvariant() switch
        {
            "success"  => "success",
            "failed"   => "failed",
            "refunded" => "refunded",
            _          => null
        };
    }
}

// ── Webhook payload DTO ───────────────────────────────────────────────────────

public sealed record SePayWebhookDto(
    [property: JsonPropertyName("webhook_id")]          string?  WebhookId,
    [property: JsonPropertyName("payment_code")]        string   PaymentCode,
    [property: JsonPropertyName("transaction_status")]  string   TransactionStatus,
    [property: JsonPropertyName("transaction_id")]      string   TransactionId,
    [property: JsonPropertyName("amount")]              decimal  Amount,
    [property: JsonPropertyName("seller_id")]           Guid?    SellerId
);
