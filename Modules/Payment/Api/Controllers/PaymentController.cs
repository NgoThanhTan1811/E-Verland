using System.Security.Cryptography;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Net;
using Infra.AWS.CloudWatch;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
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
    ICloudWatchService cloudWatch,
    IConfiguration configuration,
    IWebHostEnvironment hostEnvironment,
    ILogger<PaymentController> logger) : ControllerBase
{
    private readonly IMediator _mediator = mediator;
    private readonly ICloudWatchService _cloudWatch = cloudWatch;
    private readonly IConfiguration _configuration = configuration;
    private readonly IWebHostEnvironment _hostEnvironment = hostEnvironment;
    private readonly ILogger<PaymentController> _logger = logger;

    // ── Commands ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Debug endpoint — DEVELOPMENT ONLY. Verifies SePay HMAC signature and returns computed hash.
    /// POST /api/payment/webhook/debug-signature
    /// Headers: X-SePay-Timestamp, X-SePay-Signature
    /// Body: raw JSON payload
    /// </summary>
    [AllowAnonymous]
    [HttpPost("webhook/debug-signature")]
    [DisableRequestSizeLimit]
    [ApiExplorerSettings(IgnoreApi = false)]
    public async Task<IActionResult> DebugSignature(CancellationToken ct)
    {
        if (!_hostEnvironment.IsDevelopment())
            return NotFound();

        Request.EnableBuffering();
        using var ms = new MemoryStream();
        await Request.Body.CopyToAsync(ms, ct);
        var rawBodyBytes = ms.ToArray();
        var rawBody = Encoding.UTF8.GetString(rawBodyBytes);

        var sepayKey = _configuration["SePay:SecretKey"] ?? string.Empty;
        var timestampHeader = Request.Headers["X-SePay-Timestamp"].ToString();
        var receivedSig = NormalizeSePaySignatureHeader(Request.Headers["X-SePay-Signature"].ToString());

        string? computedHex = null;
        if (!string.IsNullOrWhiteSpace(sepayKey) &&
            long.TryParse(timestampHeader, out var ts))
        {
            var signed = BuildSePaySignedPayloadBytes(ts, rawBodyBytes);
            computedHex = Convert.ToHexString(HMACSHA256.HashData(
                Encoding.UTF8.GetBytes(sepayKey), signed)).ToLowerInvariant();
        }

        return Ok(new
        {
            bodyLengthBytes = rawBodyBytes.Length,
            bodyPreview = rawBody.Length > 120 ? rawBody[..120] + "..." : rawBody,
            timestamp = timestampHeader,
            receivedSignature = receivedSig,
            computedSignature = computedHex,
            match = computedHex != null && computedHex == receivedSig,
            secretKeyConfigured = !string.IsNullOrWhiteSpace(sepayKey),
            secretKeyPrefix = sepayKey.Length > 8 ? sepayKey[..8] + "..." : "(too short)"
        });
    }

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
        catch (ArgumentException ex)
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

    [Authorize(Policy = "AdminPolicy")]
    [HttpGet("sepay/transactions")]
    public async Task<ActionResult<Modules.Payment.Application.DTOs.Response.SePayTransactionsResponseDto>> GetSePayTransactions(
        [FromQuery(Name = "account_number")] string? accountNumber,
        [FromQuery(Name = "transaction_date_min")] DateOnly? transactionDateMin,
        [FromQuery(Name = "transaction_date_max")] DateOnly? transactionDateMax,
        [FromQuery(Name = "since_id")] long? sinceId,
        [FromQuery(Name = "limit")] int? limit,
        [FromQuery(Name = "reference_number")] string? referenceNumber,
        [FromQuery(Name = "amount_in")] decimal? amountIn,
        [FromQuery(Name = "amount_out")] decimal? amountOut,
        CancellationToken ct)
    {
        var result = await _mediator.Send(new Modules.Payment.Application.Queries.GetSePayTransactionsQuery(
            accountNumber,
            transactionDateMin,
            transactionDateMax,
            sinceId,
            limit,
            referenceNumber,
            amountIn,
            amountOut), ct);

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

        _logger.LogInformation("SePay webhook received from {SourceIp}", 
            Request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown");

        if (!IsAllowedSePaySourceIp())
        {
            await _cloudWatch.PutMetricAsync("payment.webhook.failed", 1, "Count", ct: ct);
            _logger.LogWarning("Rejected SePay webhook: source IP {SourceIp} is not in allowlist",
                Request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown");
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Source IP is not allowed" });
        }

        // ── Read raw body bytes (body already buffered by pipeline middleware) ──
        using var bodyStream = new MemoryStream();
        await Request.Body.CopyToAsync(bodyStream, ct);
        Request.Body.Position = 0;
        var rawBodyBytes = bodyStream.ToArray();
        var rawBody = Encoding.UTF8.GetString(rawBodyBytes);

        // ── Verify HMAC-SHA256 signature (optional — only if SecretKey configured AND header present) ──
        // SePay only sends X-SePay-Signature when webhook signing is enabled in SePay dashboard.
        // If the header is absent, we rely on IP allowlist for security instead.
        var sepayKey = _configuration["SePay:SecretKey"] ?? string.Empty;
        var signatureHeader = Request.Headers["X-SePay-Signature"].ToString();
        var timestampHeader = Request.Headers["X-SePay-Timestamp"].ToString();

        var shouldVerify = !string.IsNullOrWhiteSpace(sepayKey)
                           && !string.IsNullOrWhiteSpace(signatureHeader);

        if (shouldVerify)
        {
            if (string.IsNullOrWhiteSpace(timestampHeader) ||
                !long.TryParse(timestampHeader, NumberStyles.Integer, CultureInfo.InvariantCulture, out var tsSeconds))
            {
                await _cloudWatch.PutMetricAsync("payment.webhook.failed", 1, "Count", ct: ct);
                _logger.LogWarning("Rejected SePay webhook: X-SePay-Signature present but X-SePay-Timestamp missing/invalid");
                return BadRequest(new { message = "Missing or invalid X-SePay-Timestamp" });
            }

            var currentTs = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (Math.Abs(currentTs - tsSeconds) > 300)
            {
                await _cloudWatch.PutMetricAsync("payment.webhook.failed", 1, "Count", ct: ct);
                _logger.LogWarning("Rejected SePay webhook: timestamp skew {Delta}s exceeds 300s", currentTs - tsSeconds);
                return BadRequest(new { message = "Timestamp too old" });
            }

            var receivedSig = NormalizeSePaySignatureHeader(signatureHeader);
            var signedBytes = BuildSePaySignedPayloadBytes(tsSeconds, rawBodyBytes);
            var computedBytes = HMACSHA256.HashData(Encoding.UTF8.GetBytes(sepayKey), signedBytes);
            var computedHex = Convert.ToHexString(computedBytes).ToLowerInvariant();

            // Log enough detail to diagnose any mismatch:
            // - bodyLen: how many bytes we received (must match what SePay signed)
            // - signedLen: timestamp + "." + body length
            // - bodyFirst32: first 32 chars of raw body (check for BOM, leading spaces, encoding issues)
            // - signedFirst20: first 20 chars of signed payload (must start with "{timestamp}.")
            _logger.LogWarning(
                "SePay HMAC — bodyLen={Len} signedLen={SignedLen} bodyFirst32={BodyHead} signedFirst20={SignedHead} computed={Computed} received={Received}",
                rawBodyBytes.Length,
                signedBytes.Length,
                rawBody.Length >= 32 ? rawBody[..32] : rawBody,
                Encoding.UTF8.GetString(signedBytes, 0, Math.Min(20, signedBytes.Length)),
                computedHex,
                receivedSig);

            if (!TryParseHex(receivedSig, out var receivedBytes) ||
                !CryptographicOperations.FixedTimeEquals(computedBytes, receivedBytes))
            {
                await _cloudWatch.PutMetricAsync("payment.webhook.failed", 1, "Count", ct: ct);
                _logger.LogWarning("Rejected SePay webhook: HMAC mismatch. computed={Computed}", computedHex);
                return Unauthorized(new { message = "Invalid signature" });
            }

            _logger.LogInformation("SePay webhook signature verified OK");
        }
        else if (string.IsNullOrWhiteSpace(sepayKey))
        {
            _logger.LogWarning("SePay webhook: SecretKey not configured — skipping signature verification. Relying on IP allowlist only.");
        }
        else
        {
            // SecretKey configured but SePay did not send signature header — accept (signing not enabled on SePay side)
            _logger.LogInformation("SePay webhook: no X-SePay-Signature header — signature verification skipped (not enabled in SePay dashboard).");
        }

        // ── Parse payload ─────────────────────────────────────────────────────
        JsonDocument payload;
        try
        {
            payload = JsonDocument.Parse(rawBody);
        }
        catch
        {
            await _cloudWatch.PutMetricAsync("payment.webhook.failed", 1, "Count", ct: ct);
            _logger.LogWarning("Rejected SePay webhook: payload could not be parsed");
            return BadRequest(new { message = "Invalid payload" });
        }

        using (payload)
        {
            var payloadRoot = payload.RootElement;
            var paymentCode = ResolvePaymentCode(payloadRoot);
            if (string.IsNullOrWhiteSpace(paymentCode))
            {
                await _cloudWatch.PutMetricAsync("payment.webhook.failed", 1, "Count", ct: ct);
                _logger.LogWarning("Rejected SePay webhook: missing payment code");
                return BadRequest(new { message = "Missing payment_code" });
            }

            var normalizedStatus = ResolveTransactionStatus(payloadRoot);
            if (normalizedStatus is null)
            {
                await _cloudWatch.PutMetricAsync("payment.webhook.failed", 1, "Count", ct: ct);
                _logger.LogWarning("Rejected SePay webhook: unsupported transaction status for {PaymentCode}",
                    paymentCode);
                return BadRequest(new
                {
                    message = "Unsupported transaction_status. Allowed: success, failed, refunded."
                });
            }

            var amount = ResolveAmount(payloadRoot);
            if (amount is null || amount <= 0)
            {
                await _cloudWatch.PutMetricAsync("payment.webhook.failed", 1, "Count", ct: ct);
                _logger.LogWarning("Rejected SePay webhook: missing or invalid amount for {PaymentCode}", paymentCode);
                return BadRequest(new { message = "Missing amount" });
            }

            // ── Dispatch to handler ───────────────────────────────────────────
            var transactionId = ResolveTransactionId(payloadRoot, paymentCode);

            try
            {
                var command = new ProcessSePayWebhookCommand(
                    TransactionId: transactionId,
                    PaymentCode: paymentCode,
                    TransactionStatus: normalizedStatus,
                    Amount: amount.Value,
                    SellerId: ResolveSellerId(payloadRoot));

                var result = await _mediator.Send(command, ct);
                return Ok(new { success = result.Success, compensated = result.Compensated });
            }
            catch (KeyNotFoundException ex)
            {
                await _cloudWatch.PutMetricAsync("payment.webhook.failed", 1, "Count", ct: ct);
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                await _cloudWatch.PutMetricAsync("payment.webhook.failed", 1, "Count", ct: ct);
                _logger.LogWarning(ex, "Rejected SePay webhook for {PaymentCode} due to validation mismatch", paymentCode);
                return BadRequest(new
                {
                    code = "PAYMENT_WEBHOOK_MISMATCH",
                    message = ex.Message
                });
            }
            catch (AggregateException ex)
            {
                _logger.LogError(ex, "Webhook processing and compensation both failed for {PaymentCode}", paymentCode);
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
                _logger.LogError(ex, "Webhook processing failed for {PaymentCode}", paymentCode);
                return BadRequest(new
                {
                    code = "PAYMENT_PROCESSING_FAILED",
                    message = "Payment processing failed and has been compensated.",
                    detail = ex.Message
                });
            }
        }
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private static bool TryParseHex(string hex, out byte[] bytes)
    {
        try { bytes = Convert.FromHexString(hex); return true; }
        catch { bytes = []; return false; }
    }

    private static string? NormalizeTransactionStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status)) return null;
        return status.Trim().ToLowerInvariant() switch
        {
            "success" => "success",
            "failed" => "failed",
            "refunded" => "refunded",
            _ => null
        };
    }

    private static string? ResolvePaymentCode(JsonElement payload)
    {
        var paymentCode = ReadString(payload, "payment_code", "paymentCode", "code", "PaymentCode", "des", "description", "Description");
        if (!string.IsNullOrWhiteSpace(paymentCode))
        {
            return paymentCode;
        }

        var content = ReadString(payload, "content", "Content");
        if (string.IsNullOrWhiteSpace(content))
        {
            return null;
        }

        var extractedCode = Regex.Match(content, "PAY\\d{8,9}", RegexOptions.IgnoreCase);
        return extractedCode.Success ? extractedCode.Value : content;
    }

    private static string NormalizeSePaySignatureHeader(string signature)
    {
        if (string.IsNullOrWhiteSpace(signature))
        {
            return string.Empty;
        }

        var normalized = signature.Trim();
        if (normalized.StartsWith("sha256=", StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized[7..].Trim();
        }

        return normalized.Replace(" ", string.Empty);
    }

    private static byte[] BuildSePaySignedPayloadBytes(long timestampSeconds, byte[] rawBodyBytes)
    {
        var timestampBytes = Encoding.UTF8.GetBytes(timestampSeconds.ToString(CultureInfo.InvariantCulture));
        var signedPayloadBytes = new byte[timestampBytes.Length + 1 + rawBodyBytes.Length];

        Buffer.BlockCopy(timestampBytes, 0, signedPayloadBytes, 0, timestampBytes.Length);
        signedPayloadBytes[timestampBytes.Length] = (byte)'.';
        Buffer.BlockCopy(rawBodyBytes, 0, signedPayloadBytes, timestampBytes.Length + 1, rawBodyBytes.Length);

        return signedPayloadBytes;
    }

    private bool IsAllowedSePaySourceIp()
    {
        var allowedIpStrings = _configuration.GetSection("SePay:AllowedIps").Get<string[]>()
            ?? _configuration.GetSection("Payment:SePay:AllowedIps").Get<string[]>();

        if (allowedIpStrings is null || allowedIpStrings.Length == 0)
        {
            return true;
        }

        var sourceIp = Request.HttpContext.Connection.RemoteIpAddress;
        if (sourceIp is null)
        {
            return false;
        }

        var normalizedSourceIp = NormalizeIpAddress(sourceIp);
        foreach (var allowedIpString in allowedIpStrings)
        {
            if (!IPAddress.TryParse(allowedIpString, out var allowedIp))
            {
                continue;
            }

            if (NormalizeIpAddress(allowedIp).Equals(normalizedSourceIp))
            {
                return true;
            }
        }

        return false;
    }

    private static IPAddress NormalizeIpAddress(IPAddress ipAddress)
    {
        return ipAddress.MapToIPv6();
    }

    private static string? ResolveTransactionStatus(JsonElement payload)
    {
        var explicitStatus = NormalizeTransactionStatus(ReadString(payload, "transaction_status", "transactionStatus", "TransactionStatus"));
        if (explicitStatus is not null)
        {
            return explicitStatus;
        }

        var transferType = ReadString(payload, "transferType", "transfer_type", "TransferType");
        if (string.IsNullOrWhiteSpace(transferType))
        {
            return null;
        }

        return transferType.Trim().ToLowerInvariant() switch
        {
            "in" => "success",
            "out" => "failed",
            _ => null
        };
    }

    private static decimal? ResolveAmount(JsonElement payload)
    {
        var amount = ReadDecimal(payload, "amount", "Amount");
        if (amount is not null)
        {
            return amount;
        }

        return ReadDecimal(payload, "transferAmount", "transfer_amount", "TransferAmount");
    }

    private static string ResolveTransactionId(JsonElement payload, string paymentCode)
    {
        return ReadString(payload, "id", "transaction_id", "transactionId", "TransactionId")
            ?? ReadString(payload, "referenceCode", "reference_code", "ReferenceCode")
            ?? ReadString(payload, "code", "payment_code", "paymentCode", "PaymentCode")
            ?? paymentCode;
    }

    private static Guid? ResolveSellerId(JsonElement payload)
    {
        var sellerIdText = ReadString(payload, "seller_id", "sellerId", "SellerId");
        return Guid.TryParse(sellerIdText, out var sellerId) ? sellerId : null;
    }

    private static string? ReadString(JsonElement payload, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            if (!payload.TryGetProperty(propertyName, out var property))
            {
                continue;
            }

            if (property.ValueKind == JsonValueKind.String)
            {
                var value = property.GetString();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }
            else if (property.ValueKind is JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False)
            {
                return property.ToString();
            }
        }

        return null;
    }

    private static decimal? ReadDecimal(JsonElement payload, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            if (!payload.TryGetProperty(propertyName, out var property))
            {
                continue;
            }

            if (property.ValueKind == JsonValueKind.Number && property.TryGetDecimal(out var decimalValue))
            {
                return decimalValue;
            }

            if (property.ValueKind == JsonValueKind.String &&
                decimal.TryParse(property.GetString(), out var parsedValue))
            {
                return parsedValue;
            }
        }

        return null;
    }
}

