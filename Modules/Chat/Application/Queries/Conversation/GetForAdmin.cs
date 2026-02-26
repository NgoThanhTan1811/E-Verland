using MediatR;
using Modules.Chat.Application.Contracts;
using Modules.Chat.Application.DTOs.Response;

namespace Modules.Chat.Application.Queries.Conversation;

public sealed record GetForAdminQuery(Guid AdminId) : IRequest<List<ConversationResponseDto>>;

public sealed class GetForAdminQueryHandler(IConversationRepository conversationRepo)
        : IRequestHandler<GetForAdminQuery, List<ConversationResponseDto>>
{
    private readonly IConversationRepository _conversationRepo = conversationRepo;

    public async Task<List<ConversationResponseDto>> Handle(GetForAdminQuery req, CancellationToken ct)
    {
        if (req.AdminId == Guid.Empty)
            throw new ArgumentException("AdminId is required.");

        var conversations = await _conversationRepo.GetConversationsForAdminAsync(req.AdminId, ct);

        return [.. conversations.Select(c => new ConversationResponseDto(
                c.Id,
                c.UserId,
                c.AdminId,
                c.CreatedAtUtc
            ))];
    }
}