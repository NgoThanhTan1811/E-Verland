using MediatR;
using Microsoft.AspNetCore.Mvc;
using Modules.User.Application.Commands;
using Modules.User.Application.DTOs.Request;
using Modules.User.Application.DTOs.Response;
using Modules.User.Application.Queries.Address;
using Modules.User.Domain.Enums;

namespace Modules.User.Api.Controllers;

[ApiController]
[Route("api/profile/{profileId}/[controller]")]
public class AddressController(IMediator mediator) : ControllerBase
{
    private readonly IMediator _mediator = mediator;

    [HttpPost]
    [ProducesResponseType(typeof(AddressResDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AddressResDto>> CreateAddress(Guid profileId, [FromBody] CreateAddressReqDto dto, CancellationToken ct)
    {
        try
        {
            var command = new CreateAddressCommand(
                profileId,
                dto.Street,
                dto.City,
                dto.Ward,
                dto.Detail,
                dto.District,
                dto.Province,
                dto.Label,
                false
            );
            var result = await _mediator.Send(command, ct);
            return CreatedAtAction(nameof(GetAddressById), new { profileId, id = result.Id }, result);
        }
        catch (System.ComponentModel.DataAnnotations.ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(AddressResDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AddressResDto>> GetAddressById(Guid profileId, Guid id, CancellationToken ct)
    {
        try
        {
            var query = new GetAddressByIdForProfileQuery(id, profileId);
            var result = await _mediator.Send(query, ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<AddressResDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<AddressResDto>>> GetAddressesByProfile(Guid profileId, CancellationToken ct)
    {
        try
        {
            var query = new GetAddressByProfileQuery(profileId);
            var result = await _mediator.Send(query, ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpGet("default")]
    [ProducesResponseType(typeof(AddressResDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AddressResDto>> GetDefaultAddress(Guid profileId, CancellationToken ct)
    {
        try
        {
            var query = new GetAddressDefault(profileId);
            var result = await _mediator.Send(query, ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPatch("{id}")]
    [ProducesResponseType(typeof(AddressResDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AddressResDto>> UpdateAddress(Guid profileId, Guid id, [FromBody] UpdateAddressReqDto dto, CancellationToken ct)
    {
        try
        {
            var command = new UpdateAddressCommand(
                profileId,
                id,
                dto.Label,
                dto.City,
                dto.Province,
                dto.District,
                dto.Ward,
                dto.Street,
                dto.Detail,
                dto.IsDefault
            );
            var result = await _mediator.Send(command, ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (System.ComponentModel.DataAnnotations.ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAddress(Guid profileId, Guid id, CancellationToken ct)
    {
        try
        {
            var command = new DeleteAddressCommand(id);
            await _mediator.Send(command, ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
