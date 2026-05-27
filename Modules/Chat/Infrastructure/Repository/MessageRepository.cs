using MongoDB.Driver;
using Modules.Chat.Application.Contracts;
using Modules.Chat.Domain;
using Modules.Chat.Infrastructure.Persistence;

namespace Modules.Chat.Infrastructure.Repository;

public class MongoMessageRepository(ChatMongoDbContext context) : IMessageRepository
{
    private readonly IMongoCollection<Message> _messages = context.Messages;

    public async Task<Message> AddAsync(Message message, CancellationToken ct = default)
    {
        await _messages.InsertOneAsync(message, cancellationToken: ct);
        return message;
    }

    public async Task<Message?> GetByIdAsync(Guid messageId, CancellationToken ct = default)
    {
        var filter = Builders<Message>.Filter.Eq(m => m.Id, messageId);
        return await _messages.Find(filter).FirstOrDefaultAsync(ct);
    }

    public async Task<List<Message>> GetMessagesAsync(Guid conversationId, DateTime? beforeUtc = null, int take = 30, CancellationToken ct = default)
    {
        take = Math.Clamp(take, 1, 50);

        var filter = Builders<Message>.Filter.Eq(m => m.ConversationId, conversationId);

        if (beforeUtc.HasValue)
            filter &= Builders<Message>.Filter.Lt(m => m.SentAtUtc, beforeUtc.Value);

        return await _messages
            .Find(filter)
            .SortByDescending(m => m.SentAtUtc)
            .ThenByDescending(m => m.Id)
            .Limit(take)
            .ToListAsync(ct);
    }
}
