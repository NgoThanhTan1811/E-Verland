using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Modules.Chat.Application.DTOs.Response
{
    public record ConversationResponseDto
    (
        Guid Id,
        Guid UserId,
        Guid AdminId,

        DateTime CreatedAtUtc
    );

    public record ConversationListItemResponseDto
    (
        Guid Id,
        Guid UserId,

        string? LastMessagePreview,
        DateTime? LastMessageAtUtc
    );
}