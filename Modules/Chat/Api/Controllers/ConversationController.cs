
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Modules.Chat.Application.Commands.Conversation;
using Modules.Chat.Application.DTOs.Request;
using Modules.Chat.Application.Queries.Conversation;

namespace Modules.Chat.Api.Controllers;

[ApiController]
[EnableRateLimiting("chat")]
[Route("api/[controller]")]
[Authorize]
public partial class ChatController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<ChatController> _logger;

    public ChatController(IMediator mediator, ILogger<ChatController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }


    [HttpPost("conversations")]
    public async Task<IActionResult> CreateConversation(
        [FromBody] CreateConversationRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new CreateConversationCommand(request.CustomerId, request.SellerId);
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(new { conversationId = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating conversation");
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("conversations/{conversationId}")]
    public async Task<IActionResult> GetConversationById(
        Guid conversationId,
        CancellationToken cancellationToken)
    {
        try
        {
            var query = new GetConversationByIdQuery(conversationId);
            var result = await _mediator.Send(query, cancellationToken);

            if (result == null)
                return NotFound(new { message = "Conversation not found" });

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving conversation");
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("conversations/user/{userId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetConversationsForAdmin(
        Guid userId,
        CancellationToken cancellationToken)
    {
        try
        {
            var query = new GetForSellerQuery(userId);
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving conversations for admin");
            return BadRequest(new { message = ex.Message });
        }
    }

}