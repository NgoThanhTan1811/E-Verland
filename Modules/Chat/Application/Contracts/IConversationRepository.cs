namespace Modules.Chat.Application.Contracts;

public interface IConversationRepository
{
    Task<Domain.Conversation> GetOrCreateConversationAsync(Guid customerId, Guid sellerId, CancellationToken ct = default);

    Task<Domain.Conversation?> GetConversationByIdAsync(Guid conversationId, CancellationToken ct = default);

    Task<Domain.Conversation?> GetConversationByUserAsync(Guid customerId, Guid sellerId, CancellationToken ct = default);

    Task<List<Domain.Conversation>> GetConversationsForSellerAsync(Guid sellerId, CancellationToken ct = default);
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
