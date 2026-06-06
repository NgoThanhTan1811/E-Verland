using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Modules.Product.Application.Commands;
using Modules.Product.Application.DTOs.Request;
using Modules.Product.Application.Queries;

namespace Modules.Product.Api.Controllers;

[ApiController]
[EnableRateLimiting("product")]
[Route("api/[controller]")]
public class SkuController(IMediator mediator) : ControllerBase
{
    private readonly IMediator _mediator = mediator;

    [HttpPost]
    [Authorize(Policy = "AdminOrSeller")]
    public async Task<IActionResult> CreateSku([FromBody] CreateSkuRequestDto request, CancellationToken cancellationToken)
    {
        var (userId, role, shopName) = GetCurrentUser();
        var command = new CreateSkuCommand(request);
        var result = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetSkuById), new { id = result.Id }, result);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "AdminOrSeller")]
    public async Task<IActionResult> UpdateSku(Guid id, [FromBody] UpdateSkuRequestDto request, CancellationToken cancellationToken)
    {
        var (userId, role, shopName) = GetCurrentUser();
        var command = new UpdateSkuCommand(id, request);
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpPost("products/{productId:guid}/skus")]
    [Authorize(Policy = "AdminOrSeller")]
    public async Task<IActionResult> AddSkusToProduct(Guid productId, [FromBody] AddSkusToProductRequestDto request, CancellationToken cancellationToken)
    {   
        var (userId, role, shopName) = GetCurrentUser();
        var command = new AddSkusToProductCommand(productId, request.Variants, request.Stock);
        var result = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetSkuById), new { id = result.FirstOrDefault()?.Id ?? Guid.Empty }, result);
    }

    [Authorize(Policy = "AdminOrSeller")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteSku(Guid id, CancellationToken cancellationToken)
    {
        var command = new DeleteSkuCommand(id);
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetSkuById(Guid id, CancellationToken cancellationToken)
    {
        var query = new GetSkuByIdQuery(id);
        var result = await _mediator.Send(query, cancellationToken);
        if (result == null)
            return NotFound();
        return Ok(result);
    }

    [Authorize(Policy = "AdminOrSeller")]
    [HttpGet("admin/search")]
    public async Task<IActionResult> SearchSkus([FromQuery] SearchSkuAdminRequestDto filter, CancellationToken cancellationToken)
    {
        var (userId, role, shopName) = GetCurrentUser();
        var query = new SearchSkuAdminQuery(filter);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    private (Guid UserId, string Role, string ShopName) GetCurrentUser()
    {
        var userIdRaw = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!Guid.TryParse(userIdRaw, out var userId))
        {
            throw new UnauthorizedAccessException("Invalid token.");
        }

        var role = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
        var shopName = User.FindFirstValue(ClaimTypes.Name) ?? string.Empty;
        return (userId, role, shopName);
    }
}
