namespace Modules.Chat.Application.Contracts;

public interface IConversationRepository
{
    Task<Domain.Conversation> GetOrCreateConversationAsync(Guid userId, Guid adminId, CancellationToken ct = default);

    Task<Domain.Conversation?> GetConversationByIdAsync(Guid conversationId, CancellationToken ct = default);

    Task<Domain.Conversation?> GetConversationByUserAsync(Guid userId, Guid adminId, CancellationToken ct = default);

    Task<List<Domain.Conversation>> GetConversationsForAdminAsync(Guid adminId, CancellationToken ct = default);
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}