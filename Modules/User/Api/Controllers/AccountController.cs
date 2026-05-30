using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Modules.User.Application.Commands;
using Modules.User.Application.DTOs.Request;
using Modules.User.Application.DTOs.Response;
using Modules.User.Application.Queries;
using Modules.User.Application.Queries.Account;
using SharedKernel.Pagination;

namespace Modules.User.Api.Controllers;

[ApiController]
[EnableRateLimiting("user")]
[Route("api/[controller]")]
[Authorize]
public class AccountController(IMediator mediator) : ControllerBase
{
    private readonly IMediator _mediator = mediator;

    [HttpPost]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<ActionResult<AccountResDto>> CreateAccount([FromBody] CreateAccountReqDto dto, CancellationToken ct)
    {
        try
        {
            var command = new CreateAcountCommand(dto.Email, dto.Username, dto.Password);
            var result = await _mediator.Send(command, ct);
            return CreatedAtAction(nameof(GetAccountById), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (System.ComponentModel.DataAnnotations.ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // [HttpPut("me")]
    // [Authorize]
    // public async Task<ActionResult<AccountResDto>> UpdateMe([FromBody] UpdateMyAccountReqDto dto, CancellationToken ct)
    // {
    //     try
    //     {
    //         var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    //         if (!Guid.TryParse(currentUserId, out var accountId))
    //         {
    //             return Unauthorized(new { message = "Invalid token." });
    //         }

    //         var command = new UpdateAccountCommand(accountId, dto.Username, dto.Password, null, null);
    //         var result = await _mediator.Send(command, ct);
    //         return Ok(result);
    //     }
    //     catch (KeyNotFoundException ex)
    //     {
    //         return NotFound(new { message = ex.Message });
    //     }
    //     catch (System.ComponentModel.DataAnnotations.ValidationException ex)
    //     {
    //         return BadRequest(new { message = ex.Message });
    //     }
    //     catch (InvalidOperationException ex)
    //     {
    //         return Conflict(new { message = ex.Message });
    //     }
    //     catch (ArgumentException ex)
    //     {
    //         return BadRequest(new { message = ex.Message });
    //     }
    // }

    [HttpGet("{id}")]
    [Authorize]
    public async Task<ActionResult<AccountResDto>> GetAccountById(Guid id, CancellationToken ct)
    {
        try
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;

            if (currentUserRole != "Admin" && currentUserId != id.ToString())
            {
                return Forbid();
            }

            var query = new GetAccountQuery(id);
            var result = await _mediator.Send(query, ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<AccountMeResDto>> GetMe(CancellationToken ct)
    {
        try
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(currentUserId, out var accountId))
            {
                return Unauthorized(new { message = "Invalid token." });
            }

            var query = new GetMeQuery(accountId);
            var result = await _mediator.Send(query, ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpGet("email/{email}")]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<ActionResult<AccountResDto>> GetAccountByEmail(string email, CancellationToken ct)
    {
        try
        {
            var query = new GetAccountByEmailQuery(email);
            var result = await _mediator.Send(query, ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpGet("username/{username}")]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<ActionResult<AccountResDto>> GetAccountByUsername(string username, CancellationToken ct)
    {
        try
        {
            var query = new GetAccountByUserNameQuery(username);
            var result = await _mediator.Send(query, ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpGet]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<ActionResult<PageResult<AccountResDto>>> GetAccounts([FromQuery] AccountFilter filter, CancellationToken ct)
    {
        try
        {
            var query = new GetAccountsQuery(filter);
            var result = await _mediator.Send(query, ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPatch("{id}")]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<ActionResult<AccountResDto>> UpdateAccount(Guid id, [FromBody] UpdateAccountReqDto dto, CancellationToken ct)
    {
        try
        {
            var command = new UpdateAccountCommand(id, dto.Username, dto.Password, dto.Role, dto.Status);
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
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<IActionResult> DeleteAccount(Guid id, CancellationToken ct)
    {
        try
        {
            var command = new DeleteAccountCommand(id);
            await _mediator.Send(command, ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
