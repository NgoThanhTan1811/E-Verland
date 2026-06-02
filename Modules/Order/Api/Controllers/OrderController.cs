using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Modules.Order.Application.Commands;
using Modules.Order.Application.DTOs.Request;
using Modules.Order.Application.DTOs.Response;
using Modules.Order.Application.Queries;
using Modules.Order.Domain;
using SharedKernel.Pagination;
using System.Security.Claims;

namespace Modules.Order.Api.Controllers;

[ApiController]
[EnableRateLimiting("order")]
[Route("api/[controller]")]
public class OrderController : ControllerBase
{
    private readonly IMediator _mediator;

    public OrderController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [Authorize(Policy = "CustomerPolicy")]
    [HttpPost]
    public async Task<ActionResult<CreateOrderResponseDto>> CreateOrder(
        [FromBody] CreateOrderRequestDto dto,
        CancellationToken ct)
    {
        try
        {
            var userId = GetCurrentUserId();

            var command = new CreateOrderCommand(
                userId,
                dto.ShippingAddressId,
                dto.ShippingAddress,
                dto.Receiver,
                dto.Weight,
                dto.Length,
                dto.Width,
                dto.Height,
                dto.ServiceId,
                dto.ServiceTypeId,
                dto.InsuranceValue,
                dto.Note,
                dto.RequiredNote,
                dto.PaymentMethod,
                dto.VoucherCode,
                dto.Items
            );

            var result = await _mediator.Send(command, ct);
            return CreatedAtAction(nameof(GetOrderById), new { id = result.Id }, result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize(Policy = "SellerOrCustomer")]
    [HttpGet("{id}")]
    public async Task<ActionResult<OrderDetailResponseDto>> GetOrderById(Guid id, CancellationToken ct)
    {
        try
        {
            var userId = GetCurrentUserId();
            var query = new GetOrderByIdQuery(id, userId);
            var result = await _mediator.Send(query, ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [Authorize(Policy = "SellerOrCustomer")]
    [HttpGet]
    public async Task<ActionResult<PageResult<OrderOverviewResponseDto>>> GetOrders(
        [FromQuery] OrderStatus? status,
        [FromQuery] PaymentStatus? paymentStatus,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] int? page,
        [FromQuery] int? limit,
        CancellationToken ct)
    {
        try
        {
            var userId = GetCurrentUserId();

            var filterDto = new FilterOrdersUserRequestDto(
                status,
                paymentStatus,
                fromDate,
                toDate,
                page,
                limit
            );
            var query = new FilterOrdersUserQuery(userId, filterDto);
            var result = await _mediator.Send(query, ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [Authorize(Policy = "AdminPolicy")]
    [HttpGet("admin/order")]
    public async Task<ActionResult<PageResult<OrderOverviewResponseDto>>> FilterOrdersAdmin(
        [FromQuery] Guid? userId,
        [FromQuery] OrderStatus? status,
        [FromQuery] PaymentStatus? paymentStatus,
        [FromQuery] PaymentMethod? paymentMethod,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] int? page,
        [FromQuery] int? limit,
        CancellationToken ct)
    {
        try
        {
            var filterDto = new FilterOrdersAdminRequestDto(
                userId,
                status,
                paymentStatus,
                paymentMethod,
                fromDate,
                toDate,
                page,
                limit
            );
            var query = new FilterOrdersAdminQuery(filterDto);
            var result = await _mediator.Send(query, ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [Authorize(Policy = "SellerOrCustomer")]
    [HttpPatch("{id}")]
    public async Task<ActionResult<OrderOverviewResponseDto>> UpdateOrderStatus(
        Guid id,
        [FromBody] UpdateOrderStatusRequest request,
        CancellationToken ct)
    {
        try
        {
            var command = new UpdateOrderStatusCommand(id, request.Status);
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

    [Authorize(Policy = "AdminPolicy")]
    [HttpPatch("admin/{id}/status")]
    public async Task<ActionResult<OrderOverviewResponseDto>> AdminUpdateOrderStatus(
        Guid id,
        [FromBody] UpdateOrderStatusRequest request,
        CancellationToken ct)
    {
        try
        {
            var command = new UpdateOrderStatusCommand(id, request.Status);
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

    [Authorize]
    [HttpDelete("{id}")]
    public async Task<IActionResult> CancelOrder(Guid id, CancellationToken ct)
    {
        try
        {
            var userId = GetCurrentUserId();
            var command = new CancelOrderCommand(id, userId);
            await _mediator.Send(command, ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize(Policy = "AdminPolicy")]
    [HttpDelete("admin/{id}")]
    public async Task<IActionResult> AdminCancelOrder(Guid id, CancellationToken ct)
    {
        try
        {
            var command = new CancelOrderCommand(id, null, BypassOwnershipCheck: true);
            await _mediator.Send(command, ct);
            return NoContent();
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

    private Guid GetCurrentUserId()
    {
        var userIdRaw = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!Guid.TryParse(userIdRaw, out var userId))
        {
            throw new UnauthorizedAccessException("Invalid token.");
        }

        return userId;
    }
}

public record UpdateOrderStatusRequest(OrderStatus Status);
