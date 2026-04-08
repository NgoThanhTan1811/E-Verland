namespace Modules.Chat.Application.DTOs.Request;

public record CreateConversationRequestDto(Guid CustomerId, Guid SellerId);

public record AddMessageRequestDto(Guid ConversationId, Guid SenderId, string Content);
