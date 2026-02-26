namespace Modules.Chat.Application.DTOs.Request;

public record CreateConversationRequestDto(Guid UserId, Guid AdminId);

public record AddMessageRequestDto(Guid ConversationId, Guid SenderId, string Content);
