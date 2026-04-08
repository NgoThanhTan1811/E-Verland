using MediatR;
using Modules.Chat.Application.Contracts;
using Modules.Chat.Application.DTOs.Response;

namespace Modules.Chat.Application.Queries.Conversation;

public sealed record GetForSellerQuery(Guid SellerId) : IRequest<List<ConversationResponseDto>>;

public sealed class GetForSellerQueryHandler(IConversationRepository conversationRepo)
        : IRequestHandler<GetForSellerQuery, List<ConversationResponseDto>>
{
    private readonly IConversationRepository _conversationRepo = conversationRepo;

    public async Task<List<ConversationResponseDto>> Handle(GetForSellerQuery req, CancellationToken ct)
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
