using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Modules.Chat.Application.DTOs.Response
{
    public record MessageResponseDto
    (
        Guid Id,
        Guid ConversationId,
        Guid SenderId,
        string Content,
        DateTime SentAtUtc
    );
    
    public record SendMessageResponseDto
    (
        Guid MessageId,
        Guid ConversationId,
        DateTime SentAtUtc
    );
}