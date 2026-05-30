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
    [Authorize(Policy = "AdminPolicy")]
    public async Task<IActionResult> CreateSku([FromBody] CreateSkuRequestDto request, CancellationToken cancellationToken)
    {
        var command = new CreateSkuCommand(request);
        var result = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetSkuById), new { id = result.Id }, result);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<IActionResult> UpdateSku(Guid id, [FromBody] UpdateSkuRequestDto request, CancellationToken cancellationToken)
    {
        var command = new UpdateSkuCommand(id, request);
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpPost("products/{productId:guid}/skus")]
    [Authorize(Policy = "AdminOrSeller")]
    public async Task<IActionResult> AddSkusToProduct(Guid productId, [FromBody] AddSkusToProductRequestDto request, CancellationToken cancellationToken)
    {
        var command = new AddSkusToProductCommand(productId, request.Variants);
        var result = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetSkuById), new { id = result.FirstOrDefault()?.Id ?? Guid.Empty }, result);
    }

    [Authorize(Policy = "AdminPolicy")]
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

    [Authorize(Policy = "AdminPolicy")]
    [HttpGet("admin/search")]
    public async Task<IActionResult> SearchSkus([FromQuery] SearchSkuAdminRequestDto filter, CancellationToken cancellationToken)
    {
        var query = new SearchSkuAdminQuery(filter);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }
}
