using MediatR;
using Modules.Chat.Application.Contracts;
using Modules.Chat.Application.DTOs.Response;

namespace Modules.Chat.Application.Queries.Conversation;

public sealed record GetForAdminQuery(Guid SellerId) : IRequest<List<ConversationResponseDto>>;

public sealed class GetForAdminQueryHandler(IConversationRepository conversationRepo)
        : IRequestHandler<GetForAdminQuery, List<ConversationResponseDto>>
{
    private readonly IConversationRepository _conversationRepo = conversationRepo;

    public async Task<List<ConversationResponseDto>> Handle(GetForAdminQuery req, CancellationToken ct)
    {
        if (req.SellerId == Guid.Empty)
            throw new ArgumentException("SellerId is required.");

        var conversations = await _conversationRepo.GetConversationsForSellerAsync(req.SellerId, ct);

        return [.. conversations.Select(c => new ConversationResponseDto(
                c.Id,
                c.CustomerId,
                c.SellerId,
                c.CreatedAtUtc
            ))];
    }
}
