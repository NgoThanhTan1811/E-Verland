using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Modules.User.Application.Commands;
using Modules.User.Application.DTOs.Request;
using Modules.User.Application.DTOs.Response;
using Modules.User.Application.Queries.Profile;
using Modules.User.Domain.Enums;
using SharedKernel.Pagination;

namespace Modules.User.Api.Controllers;

[ApiController]
[EnableRateLimiting("user")]
[Route("api/[controller]")]
public class ProfileController(IMediator mediator) : ControllerBase
{
    private readonly IMediator _mediator = mediator;
    [Authorize]
    [HttpPost]
    public async Task<ActionResult<ProfileResDto>> CreateProfile([FromBody] CreateProfileReqDto dto, [FromQuery] Guid accountId, CancellationToken ct)
    {
        try
        {
            var command = new CreateProfileCommand(
                accountId,
                dto.FirstName,
                dto.LastName,
                DateTime.UtcNow,
                dto.PhoneNumber
            );
            var result = await _mediator.Send(command, ct);
            return CreatedAtAction(nameof(GetProfileById), new { id = result.Id }, result);
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
    [Authorize]
    public async Task<ActionResult<ProfileResDto>> GetProfileById(Guid id, CancellationToken ct)
    {
        try
        {
            var query = new GetProfileByQuery(id);
            var result = await _mediator.Send(query, ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    // [HttpGet("account/{accountId}")]
    // [Authorize(Policy = "AdminPolicy")]
    // public async Task<ActionResult<ProfileResDto>> GetProfileByAccount(Guid accountId, CancellationToken ct)
    // {
    //     try
    //     {
    //         var query = new GetProfileByAccountQuery(accountId);
    //         var result = await _mediator.Send(query, ct);
    //         return Ok(result);
    //     }
    //     catch (KeyNotFoundException ex)
    //     {
    //         return NotFound(new { message = ex.Message });
    //     }
    // }

    [HttpGet]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<ActionResult<PageResult<ProfileResDto>>> GetProfiles([FromQuery] PagingFilter filter, CancellationToken ct)
    {
        try
        {
            var query = new GetManyProfileByQuery(filter);
            var result = await _mediator.Send(query, ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
    [Authorize]
    [HttpPatch("{accountId}")]
    public async Task<ActionResult<ProfileResDto>> UpdateProfile(Guid accountId, [FromBody] UpdateProfileReqDto dto, CancellationToken ct)
    {
        try
        {
            var command = new UpdateProfileCommand(
                accountId,
                dto.FirstName,
                dto.LastName,
                dto.PhoneNumber,
                dto.DateOfBirth,
                dto.AvatarUrl,
                dto.Gender,
                dto.Bio
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
    [Authorize(Policy = "AdminPolicy")]
    public async Task<IActionResult> DeleteProfile(Guid id, CancellationToken ct)
    {
        try
        {
            var command = new DeleteProfileCommand(id);
            await _mediator.Send(command, ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
