using MediatR;
using Microsoft.AspNetCore.Mvc;
using Modules.Cart.Application.Commands;
using Modules.Cart.Application.DTOs.Request;
using Modules.Cart.Application.DTOs.Response;
using Modules.Cart.Application.Queries;

namespace Modules.Cart.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CartController(IMediator mediator) : ControllerBase
{
    private readonly IMediator _mediator = mediator;


    [HttpPost("user/{userId}/items")]
    public async Task<IActionResult> AddToCart(
        Guid userId,
        [FromBody] AddToCartRequestDto request,
        CancellationToken cancellationToken)
    {
        var command = new AddToCartCommand(userId, request);
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetUserCart(Guid userId, CancellationToken cancellationToken)
    {
        var query = new GetUserCartQuery(userId);
        var result = await _mediator.Send(query, cancellationToken);

        if (result == null)
            return NotFound(new { message = "Cart not found for user" });

        return Ok(result);
    }

    [HttpGet("{cartId}")]
    public async Task<IActionResult> GetCartById(Guid cartId, CancellationToken cancellationToken)
    {
        var query = new GetCartByIdQuery(cartId);
        var result = await _mediator.Send(query, cancellationToken);

        if (result == null)
            return NotFound(new { message = "Cart not found" });

        return Ok(result);
    }


    [HttpPut("items/{cartItemId}")]
    public async Task<IActionResult> UpdateCartItem(
        Guid cartItemId,
        [FromBody] UpdateCartItemRequestDto request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateCartItemCommand(request with { CartItemId = cartItemId });
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }


    [HttpDelete("items/{cartItemId}")]
    public async Task<IActionResult> RemoveFromCart(Guid cartItemId, CancellationToken cancellationToken)
    {
        var command = new RemoveFromCartCommand(cartItemId);
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }


    [HttpDelete("{cartId}")]
    public async Task<IActionResult> ClearCart(Guid cartId, CancellationToken cancellationToken)
    {
        var command = new ClearCartCommand(cartId);
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }
}
