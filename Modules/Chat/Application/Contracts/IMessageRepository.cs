using Modules.Chat.Domain;

namespace Modules.Chat.Application.Contracts;

public interface IMessageRepository
{
    Task<Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<Message>> AddAsync(Message message, CancellationToken ct = default);

    Task<Domain.Message?> GetByIdAsync(Guid messageId, CancellationToken ct = default);

    Task<List<Domain.Message>> GetMessagesAsync(Guid conversationId, DateTime? beforeUtc = null, int take = 30, CancellationToken ct = default);

}