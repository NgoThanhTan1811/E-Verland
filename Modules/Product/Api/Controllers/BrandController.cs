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
public class BrandController(IMediator mediator) : ControllerBase
{
    private readonly IMediator _mediator = mediator;
    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> CreateBrand([FromBody] CreateBrandRequestDto request, CancellationToken cancellationToken)
    {
        var command = new CreateBrandCommand(request);
        var result = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetBrandById), new { id = result.Id }, result);
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateBrand(Guid id, [FromBody] UpdateBrandRequestDto request, CancellationToken cancellationToken)
    {
        var command = new UpdateBrandCommand(id, request);
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteBrand(Guid id, CancellationToken cancellationToken)
    {
        var command = new DeleteBrandCommand(id);
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetBrandById(Guid id, CancellationToken cancellationToken)
    {
        var query = new GetBrandByIdQuery(id);
        var result = await _mediator.Send(query, cancellationToken);
        if (result == null)
            return NotFound();
        return Ok(result);
    }

    [HttpGet("search/brand")]
    public async Task<IActionResult> SearchBrands([FromQuery] SearchBrandRequestDto filter, CancellationToken cancellationToken)
    {
        var query = new SearchBrandQuery(filter);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }
}
