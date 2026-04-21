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
public class ProductController(IMediator mediator) : ControllerBase
{
    private readonly IMediator _mediator = mediator;


    [Authorize(Policy = "AdminPolicy")]
    [HttpPost]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductRequestDto request, CancellationToken cancellationToken)
    {
        var command = new CreateProductCommand(request);
        var result = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetProductById), new { id = result.Id }, result);
    }


    [Authorize(Policy = "AdminPolicy")]
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateProduct(Guid id, [FromBody] UpdateProductRequestDto request, CancellationToken cancellationToken)
    {
        var command = new UpdateProductCommand(id, request);
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }


    [Authorize(Policy = "AdminPolicy")]
    [HttpPatch("{id}/status")]
    public async Task<IActionResult> ChangeProductStatus(Guid id, [FromBody] ChangeProductStatusRequest request, CancellationToken cancellationToken)
    {
        var command = new ChangeProductStatusCommand(id, request.Status);
        await _mediator.Send(command, cancellationToken);
        return Ok();
    }


    [Authorize(Policy = "AdminPolicy")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProduct(Guid id, CancellationToken cancellationToken)
    {
        var command = new DeleteProductCommand(id);
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }


    [HttpGet("{id}")]
    public async Task<IActionResult> GetProductById(Guid id, CancellationToken cancellationToken)
    {
        var query = new GetProductByIdQuery(id);
        var result = await _mediator.Send(query, cancellationToken);
        if (result == null)
            return NotFound();
        return Ok(result);
    }

    [Authorize(Policy = "AdminPolicy")]
    [HttpGet("admin/search")]
    public async Task<IActionResult> SearchProductsAdmin([FromQuery] FilterProductAdminRequestDto filter, CancellationToken cancellationToken)
    {
        var query = new SearchProductAdminQuery(filter);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("p/search")]
    public async Task<IActionResult> SearchProductsCustomer([FromQuery] FilterProductCustomerRequestDto filter, CancellationToken cancellationToken)
    {
        var query = new SearchProductCustomerQuery(filter);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }
}

public record ChangeProductStatusRequest(Domain.ProductStatus Status);
