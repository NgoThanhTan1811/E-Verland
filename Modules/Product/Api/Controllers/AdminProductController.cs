using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Modules.Product.Application.Commands;

namespace Modules.Product.Api.Controllers;

[ApiController]
[Authorize(Policy = "AdminPolicy")]
[Route("api/admin/products")]
public sealed class AdminProductController(IMediator mediator) : ControllerBase
{
    private readonly IMediator _mediator = mediator;

    [HttpPatch("{id:guid}/hide")]
    public async Task<IActionResult> Hide(Guid id, [FromBody] ModerationReasonRequest request, CancellationToken cancellationToken)
    {
        ValidateReason(request.Reason);
        var adminId = GetAdminId();
        await _mediator.Send(new HideProductByAdminCommand(id, adminId, request.Reason), cancellationToken);
        return Ok();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> SoftDelete(Guid id, [FromBody] ModerationReasonRequest request, CancellationToken cancellationToken)
    {
        ValidateReason(request.Reason);
        var adminId = GetAdminId();
        await _mediator.Send(new SoftDeleteProductByAdminCommand(id, adminId, request.Reason), cancellationToken);
        return NoContent();
    }

    [HttpPatch("{id:guid}/restore")]
    public async Task<IActionResult> Restore(Guid id, [FromBody] ModerationReasonRequest request, CancellationToken cancellationToken)
    {
        ValidateReason(request.Reason);
        var adminId = GetAdminId();
        await _mediator.Send(new RestoreProductByAdminCommand(id, adminId, request.Reason), cancellationToken);
        return Ok();
    }

    private Guid GetAdminId()
    {
        var rawValue = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? throw new UnauthorizedAccessException("Unable to resolve admin ID from token.");

        if (!Guid.TryParse(rawValue, out var adminId))
        {
            throw new UnauthorizedAccessException("Admin ID claim is not a valid GUID.");
        }

        return adminId;
    }

    private static void ValidateReason(string? reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Moderation reason is required.", nameof(reason));
        }
    }
}

public sealed record ModerationReasonRequest(string Reason);
