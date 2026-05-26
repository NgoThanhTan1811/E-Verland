using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Modules.Shipping.Application.Commands;
using Modules.Shipping.Application.DTOs.External;
using Modules.Shipping.Application.DTOs.Request;
using Modules.Shipping.Application.DTOs.Response;
using Modules.Shipping.Application.Queries;

namespace Modules.Shipping.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ShippingController(IMediator mediator) : ControllerBase
{
    private readonly IMediator _mediator = mediator;

    [Authorize]
    [HttpPost("draft")]
    public async Task<ActionResult<ShippingOrderResponseDto>> CreateDraft(
        [FromBody] CreateShippingDraftRequestDto dto,
        CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new CreateShippingDraftCommand(dto), ct);
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

    [Authorize]
    [HttpPost("fee")]
    public async Task<ActionResult<ShippingFeeResponseDto>> CalculateFee(
        [FromBody] CalculateShippingFeeRequestDto dto,
        CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new CalculateShippingFeeQuery(dto), ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize]
    [HttpGet("order/{orderId}")]
    public async Task<ActionResult<ShippingOrderResponseDto>> GetByOrderId(Guid orderId, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new GetShippingByOrderIdQuery(orderId), ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [Authorize]
    [HttpGet("{id}")]
    public async Task<ActionResult<ShippingOrderResponseDto>> GetById(Guid id, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new GetShippingByIdQuery(id), ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [Authorize(Policy = "AdminPolicy")]
    [HttpPost("activate/{orderId}")]
    public async Task<ActionResult<ShippingOrderResponseDto>> Activate(Guid orderId, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new ActivateShippingOrderCommand(orderId), ct);
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

    [Authorize(Policy = "AdminPolicy")]
    [HttpPost("cancel/{orderId}")]
    public async Task<ActionResult<ShippingOrderResponseDto>> Cancel(Guid orderId, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new CancelShippingOrderCommand(orderId), ct);
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
    [HttpPost("webhook/ghn")]
    public async Task<IActionResult> GhnWebhook(
        [FromBody] GhnWebhookPayload payload,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(payload.OrderCode))
        {
            return BadRequest(new { message = "Missing OrderCode" });
        }

        await _mediator.Send(new ProcessGhnWebhookCommand(payload), ct);
        return Ok(new { success = true });
    }
}
