using MediatR;
using Microsoft.AspNetCore.Mvc;
using Modules.Product.Application.Commands;
using Modules.Product.Application.DTOs.Request;
using Modules.Product.Application.Queries;

namespace Modules.Product.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoryController(IMediator mediator) : ControllerBase
{
    private readonly IMediator _mediator = mediator;

    [HttpPost]
    public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryRequestDto request, CancellationToken cancellationToken)
    {
        var command = new CreateCategoryCommand(request);
        var result = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetCategoryById), new { id = result.Id }, result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCategory(Guid id, [FromBody] UpdateCategoryRequestDto request, CancellationToken cancellationToken)
    {
        var command = new UpdateCategoryCommand(id, request);
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCategory(Guid id, CancellationToken cancellationToken)
    {
        var command = new DeleteCategoryCommand(id);
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetCategoryById(Guid id, CancellationToken cancellationToken)
    {
        var query = new GetCategoryByIdQuery(id);
        var result = await _mediator.Send(query, cancellationToken);
        if (result == null)
            return NotFound();
        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetAllCategories(CancellationToken cancellationToken)
    {
        var query = new GetAllCategoriesQuery();
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }
}
