using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Modules.Chat.Application.DTOs.Response
{
    public record ConversationResponseDto
    (
        Guid Id,
        Guid CustomerId,
        Guid SellerId,

        DateTime CreatedAtUtc
    );

    public record ConversationListItemResponseDto
    (
        Guid Id,
        Guid CustomerId,

        string? LastMessagePreview,
        DateTime? LastMessageAtUtc
    );
}