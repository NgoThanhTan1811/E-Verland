using System.Security.Cryptography;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Net;
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
    ICloudWatchService cloudWatch,
    IConfiguration configuration,
    ILogger<PaymentController> logger) : ControllerBase
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
        foreach (var header in Request.Headers)
        {
            Console.WriteLine($"Header: {header.Key} - Value: {header.Value}");
        }

        await _cloudWatch.PutMetricAsync("payment.webhook.received", 1, "Count", ct: ct);

        if (!IsAllowedSePaySourceIp())
        {
            await _cloudWatch.PutMetricAsync("payment.webhook.failed", 1, "Count", ct: ct);
            _logger.LogWarning("Rejected SePay webhook: source IP {SourceIp} is not in allowlist", Request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown");
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Source IP is not allowed" });
        }

        // ── Read raw body bytes once and reuse them for HMAC + JSON parsing ──
        Request.EnableBuffering();
        using var bodyStream = new MemoryStream();
        await Request.Body.CopyToAsync(bodyStream, ct);
        Request.Body.Position = 0;
        var rawBodyBytes = bodyStream.ToArray();
        var rawBody = Encoding.UTF8.GetString(rawBodyBytes);

        // ── Verify HMAC-SHA256 signature ──────────────────────────────────────
        var sepayKey = _configuration["SePay:SecretKey"]
            ?? string.Empty;

        if (string.IsNullOrWhiteSpace(sepayKey))
        {
            await _cloudWatch.PutMetricAsync("payment.webhook.failed", 1, "Count", ct: ct);
            _logger.LogWarning("Rejected SePay webhook: signature key not configured");
            return BadRequest(new { message = "SePay signature key is not configured" });
        }

        var timestampHeader = Request.Headers["X-SePay-Timestamp"].ToString();
        if (string.IsNullOrWhiteSpace(timestampHeader))
        {
            await _cloudWatch.PutMetricAsync("payment.webhook.failed", 1, "Count", ct: ct);
            _logger.LogWarning("Rejected SePay webhook: missing X-SePay-Timestamp header");
            return BadRequest(new { message = "Missing X-SePay-Timestamp" });
        }

        if (!long.TryParse(timestampHeader, NumberStyles.Integer, CultureInfo.InvariantCulture, out var timestampSeconds))
        {
            await _cloudWatch.PutMetricAsync("payment.webhook.failed", 1, "Count", ct: ct);
            _logger.LogWarning("Rejected SePay webhook: invalid X-SePay-Timestamp format");
            return BadRequest(new { message = "Invalid X-SePay-Timestamp" });
        }

        var currentTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        if (Math.Abs(currentTimestamp - timestampSeconds) > 300)
        {
            await _cloudWatch.PutMetricAsync("payment.webhook.failed", 1, "Count", ct: ct);
            _logger.LogWarning("Rejected SePay webhook: timestamp outside allowed skew");
            return BadRequest(new { message = "Timestamp too old" });
        }

        var payloadToHash = $"{timestampSeconds}.{rawBody}";
        _logger.LogInformation("DEBUG: Payload dùng để băm là: {Payload}", payloadToHash);
        
        var signedPayloadBytes = BuildSePaySignedPayloadBytes(timestampSeconds, rawBodyBytes);
        var computedSignatureBytes = HMACSHA256.HashData(
            Encoding.UTF8.GetBytes(sepayKey),
            signedPayloadBytes);

        var receivedSignature = NormalizeSePaySignatureHeader(Request.Headers["X-SePay-Signature"].ToString());
        if (string.IsNullOrWhiteSpace(receivedSignature))
        {
            await _cloudWatch.PutMetricAsync("payment.webhook.failed", 1, "Count", ct: ct);
            _logger.LogWarning("Rejected SePay webhook: missing X-SePay-Signature header");
            return BadRequest(new { message = "Missing X-SePay-Signature" });
        }

        byte[] receivedSignatureBytes;
        try
        {
            receivedSignatureBytes = Convert.FromHexString(receivedSignature);
        }
        catch (FormatException)
        {
            await _cloudWatch.PutMetricAsync("payment.webhook.failed", 1, "Count", ct: ct);
            _logger.LogWarning("Rejected SePay webhook: invalid X-SePay-Signature format");
            return BadRequest(new { message = "Invalid signature format" });
        }

        var signatureValid = CryptographicOperations.FixedTimeEquals(
            computedSignatureBytes,
            receivedSignatureBytes);

        if (!signatureValid)
        {
            await _cloudWatch.PutMetricAsync("payment.webhook.failed", 1, "Count", ct: ct);
            _logger.LogWarning("Rejected SePay webhook: invalid signature");
            return BadRequest(new { message = "Invalid signature" });
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

