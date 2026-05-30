using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Modules.User.Application.Commands;
using Modules.User.Application.DTOs.Request;
using Modules.User.Application.DTOs.Response;
using Modules.User.Application.Queries.Address;
using Modules.User.Domain.Enums;

namespace Modules.User.Api.Controllers;

[ApiController]
[EnableRateLimiting("user")]
[Route("api/profile/{profileId}/[controller]")]
[Authorize]
public class AddressController(IMediator mediator) : ControllerBase
{
    private readonly IMediator _mediator = mediator;

    [HttpPost]
    public async Task<ActionResult<AddressResDto>> CreateAddress(Guid profileId, [FromBody] CreateAddressReqDto dto, CancellationToken ct)
    {
        try
        {
            var command = new CreateAddressCommand(
                profileId,
                dto.Street,
                dto.Detail,
                dto.ProvinceId,
                dto.DistrictId,
                dto.WardId,
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
    public async Task<ActionResult<AddressResDto>> UpdateAddress(Guid profileId, Guid id, [FromBody] UpdateAddressReqDto dto, CancellationToken ct)
    {
        try
        {
            var command = new UpdateAddressCommand(
                profileId,
                id,
                dto.Label,
                dto.Street,
                dto.Detail,
                dto.IsDefault,
                dto.ProvinceId,
                dto.DistrictId,
                dto.WardId
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
