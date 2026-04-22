using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Modules.Dashboard.Application.DTOs;
using Modules.Dashboard.Application.Queries;

namespace Modules.Dashboard.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class DashboardController(IMediator mediator) : ControllerBase
{
    private readonly IMediator _mediator = mediator;

    [HttpGet("admin")]
    [Authorize(Policy = "AdminPolicy")]
    [ProducesResponseType(typeof(AdminDashboardDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AdminDashboardDto>> GetAdminDashboard(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetAdminDashboardQuery(), ct);
        return Ok(result);
    }

    [HttpGet("seller")]
    [Authorize(Policy = "SellerPolicy")]
    [ProducesResponseType(typeof(SellerDashboardDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<SellerDashboardDto>> GetSellerDashboard(CancellationToken ct)
    {
        var userIdRaw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdRaw, out var sellerId))
        {
            return Unauthorized(new { message = "Invalid seller identity." });
        }

        var result = await _mediator.Send(new GetSellerDashboardQuery(sellerId), ct);
        return Ok(result);
    }
}
