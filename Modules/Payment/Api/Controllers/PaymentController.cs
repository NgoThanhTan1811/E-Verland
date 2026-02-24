using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Modules.Payment.Application.Commands;
using Modules.Payment.Application.DTOs.Request;
using Modules.Payment.Application.DTOs.Response;
using Modules.Payment.Application.Queries;
using Modules.Payment.Domain;

namespace Modules.Payment.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentController(IMediator mediator) : ControllerBase
{
    private readonly IMediator _mediator = mediator;

    [Authorize]
    [HttpPost]
    [ProducesResponseType(typeof(CreatePaymentResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreatePaymentResponseDto>> CreatePayment(
        [FromBody] CreatePaymentRequestDto dto,
        CancellationToken ct)
    {
        try
        {
            var command = new CreatePaymentCommand(
                dto.OrderId,
                dto.UserId,
                dto.Amount,
                dto.Method
            );

            var result = await _mediator.Send(command, ct);
            return CreatedAtAction(nameof(GetPaymentById), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize]
    [HttpPost("process")]
    [ProducesResponseType(typeof(CreatePaymentResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreatePaymentResponseDto>> ProcessPayment(
        [FromBody] ProcessPaymentRequestDto dto,
        [FromQuery] Guid userId,
        [FromQuery] decimal amount,
        CancellationToken ct)
    {
        try
        {
            var command = new ProcessPaymentCommand(
                dto.OrderId,
                userId,
                amount,
                dto.Method
            );

            var result = await _mediator.Send(command, ct);
            return CreatedAtAction(nameof(GetPaymentById), new { id = result.Id }, result);
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

    // Get payment by payment_code
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

    /// Get all payments for a user
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

    /// Update payment status 
    [Authorize(Roles = "Admin")]
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

    /// Webhook endpoint for payment gateway callbacks (e.g., online payment confirmation)
    [AllowAnonymous]
    [HttpPost("webhook")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PaymentWebhook(
        [FromBody] PaymentWebhookDto dto,
        CancellationToken ct)
    {
        try
        {
            // Verify webhook signature/authenticity here
            // This is a placeholder - implement actual webhook verification

            var payment = await _mediator.Send(new GetPaymentByCodeQuery(dto.PaymentCode), ct);
            if (payment == null)
                return BadRequest(new { message = "Payment not found" });

            var status = dto.Success ? PaymentStatus.Success : PaymentStatus.Failed;
            await _mediator.Send(new UpdatePaymentStatusCommand(payment.Id, status), ct);

            return Ok(new { message = "Webhook processed successfully" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

/// <summary>
/// DTO for payment gateway webhook callbacks
/// </summary>
public sealed record PaymentWebhookDto(
    string PaymentCode,
    bool Success,
    string? TransactionId,
    string? Message
);
