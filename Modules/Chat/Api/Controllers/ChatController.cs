using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Modules.Chat.Application.Commands.Conversation;
using Modules.Chat.Application.Commands.Message;
using Modules.Chat.Application.DTOs.Request;
using Modules.Chat.Application.DTOs.Response;
using Modules.Chat.Application.Queries.Conversation;
using Modules.Chat.Application.Queries.Message;

namespace Modules.Chat.Api.Controllers;

[ApiController]
[EnableRateLimiting("chat")]
[Route("api/[controller]")]
[Authorize]
public class ChatController(IMediator mediator, ILogger<ChatController> logger) : ControllerBase
{
    private readonly IMediator _mediator = mediator;
    private readonly ILogger<ChatController> _logger = logger;

    #region Conversation Endpoints

    [HttpPost("conversations")]
    public async Task<IActionResult> CreateConversation(
        [FromBody] CreateConversationRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new CreateConversationCommand(request.UserId, request.AdminId);
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
            var query = new GetForAdminQuery(userId);
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving conversations for admin");
            return BadRequest(new { message = ex.Message });
        }
    }

    #endregion

    #region Message Endpoints

    [HttpPost("messages")]
    public async Task<IActionResult> AddMessageToConversation(
        [FromBody] AddMessageRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new AddMessageToConversationCommand(
                request.ConversationId,
                request.SenderId,
                request.Content);

            var result = await _mediator.Send(command, cancellationToken);
            return Ok(new { messageId = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding message");
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("messages/{messageId}")]
    public async Task<IActionResult> GetMessageById(
        Guid messageId,
        CancellationToken cancellationToken)
    {
        try
        {
            var query = new GetMessageByIdQuery(messageId);
            var result = await _mediator.Send(query, cancellationToken);

            if (result == null)
                return NotFound(new { message = "Message not found" });

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving message");
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("messages/conversation/{conversationId}")]
    public async Task<IActionResult> GetMessagesByConversationId(
        Guid conversationId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 30,
        CancellationToken cancellationToken = default)
    {
        try
        {
            pageSize = Math.Clamp(pageSize, 1, 50);
            if (page < 1) page = 1;

            var query = new GetMessagesByConversationIdQuery(conversationId);
            var result = await _mediator.Send(query, cancellationToken);

            if (result == null || result.Count == 0)
                return Ok(new List<MessageResponseDto>());

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving messages");
            return BadRequest(new { message = ex.Message });
        }
    }

    #endregion
}
