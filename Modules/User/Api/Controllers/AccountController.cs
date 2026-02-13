using MediatR;
using Microsoft.AspNetCore.Mvc;
using Modules.User.Application.Commands;
using Modules.User.Application.DTOs.Request;
using Modules.User.Application.DTOs.Response;
using Modules.User.Application.Queries;
using Modules.User.Application.Queries.Account;

namespace Modules.User.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccountController : ControllerBase
{
    private readonly IMediator _mediator;

    public AccountController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [ProducesResponseType(typeof(AccountResDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
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

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(AccountResDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AccountResDto>> GetAccountById(Guid id, CancellationToken ct)
    {
        try
        {
            var query = new GetAccountQuery(id);
            var result = await _mediator.Send(query, ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpGet("email/{email}")]
    [ProducesResponseType(typeof(AccountResDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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
    [ProducesResponseType(typeof(AccountResDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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
    [ProducesResponseType(typeof(IEnumerable<AccountResDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<AccountResDto>>> GetAccounts([FromQuery] AccountFilter filter, CancellationToken ct)
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
    [ProducesResponseType(typeof(AccountResDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
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
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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
