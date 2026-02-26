using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Modules.User.Application.Commands;
using Modules.User.Application.DTOs.Request;
using Modules.User.Application.DTOs.Response;
using Modules.User.Application.Queries.BankAccount;

namespace Modules.User.Api.Controllers;

[ApiController]
[EnableRateLimiting("user")]
[Route("api/profile/{profileId}/[controller]")]
public class BankAccountController(IMediator mediator) : ControllerBase
{
    private readonly IMediator _mediator = mediator;

    [HttpPost]
    [ProducesResponseType(typeof(BankAccountResDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BankAccountResDto>> CreateBankAccount(Guid profileId, [FromBody] CreateBankAccountReqDto dto, CancellationToken ct)
    {
        try
        {
            var command = new CreateBankAccountCommand(
                profileId,
                dto.BankName,
                dto.BankCode,
                dto.AccountNumber,
                dto.AccountHolder
            );
            var result = await _mediator.Send(command, ct);
            return CreatedAtAction(nameof(GetBankAccountById), new { profileId, id = result.Id }, result);
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

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(BankAccountResDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BankAccountResDto>> GetBankAccountById(Guid profileId, Guid id, CancellationToken ct)
    {
        try
        {
            var query = new GetBankAccountByQuery(id);
            var result = await _mediator.Send(query, ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<BankAccountResDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<BankAccountResDto>>> GetBankAccountsByProfile(Guid profileId, CancellationToken ct)
    {
        try
        {
            var query = new GetManyBankAccountsByProfileIdQuery(profileId);
            var result = await _mediator.Send(query, ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPatch("{id}")]
    [ProducesResponseType(typeof(BankAccountResDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BankAccountResDto>> UpdateBankAccount(Guid profileId, Guid id, [FromBody] UpdateBankAccountReqDto dto, CancellationToken ct)
    {
        try
        {
            var command = new UpdateBankAccountCommand(
                id,
                profileId,
                dto.BankName,
                dto.BankCode,
                dto.AccountNumber,
                dto.AccountHolder
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
    public async Task<IActionResult> DeleteBankAccount(Guid profileId, Guid id, CancellationToken ct)
    {
        try
        {
            var command = new DeleteBankAccountCommand(id, profileId);
            await _mediator.Send(command, ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
