using MongoDB.Driver;
using Modules.Chat.Application.Contracts;
using Modules.Chat.Domain;
using Modules.Chat.Infrastructure.Persistence;

namespace Modules.Chat.Infrastructure.Repository;

public class MongoConversationRepository(ChatMongoDbContext context) : IConversationRepository
{
    private readonly IMongoCollection<Conversation> _conversations = context.Conversations;
    private Conversation? _tracked;

    public async Task<Conversation?> GetConversationByIdAsync(Guid conversationId, CancellationToken ct = default)
    {
        var filter = Builders<Conversation>.Filter.Eq(c => c.Id, conversationId);
        var result = await _conversations.Find(filter).FirstOrDefaultAsync(ct);
        if (result is not null) _tracked = result;
        return result;
    }

    public async Task<Conversation?> GetConversationByUserAsync(Guid customerId, Guid sellerId, CancellationToken ct = default)
    {
        var filter = Builders<Conversation>.Filter.And(
            Builders<Conversation>.Filter.Eq(c => c.CustomerId, customerId),
            Builders<Conversation>.Filter.Eq(c => c.SellerId, sellerId));
        return await _conversations.Find(filter).FirstOrDefaultAsync(ct);
    }

    public async Task<List<Conversation>> GetConversationsForSellerAsync(Guid sellerId, CancellationToken ct = default)
    {
        var filter = Builders<Conversation>.Filter.Eq(c => c.SellerId, sellerId);
        return await _conversations.Find(filter).ToListAsync(ct);
    }

    public async Task<Conversation> GetOrCreateConversationAsync(Guid customerId, Guid sellerId, CancellationToken ct = default)
    {
        var filter = Builders<Conversation>.Filter.And(
            Builders<Conversation>.Filter.Eq(c => c.CustomerId, customerId),
            Builders<Conversation>.Filter.Eq(c => c.SellerId, sellerId));

        var existing = await _conversations.Find(filter).FirstOrDefaultAsync(ct);
        if (existing is not null)
        {
            _tracked = existing;
            return existing;
        }

        var conversation = new Conversation(customerId, sellerId);
        try
        {
            await _conversations.InsertOneAsync(conversation, cancellationToken: ct);
            _tracked = conversation;
            return conversation;
        }
        catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            // Race condition — another request inserted first, retry find
            var created = await _conversations.Find(filter).FirstAsync(ct);
            _tracked = created;
            return created;
        }
    }

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        if (_tracked is null) return 0;

        var filter = Builders<Conversation>.Filter.Eq(c => c.Id, _tracked.Id);
        await _conversations.ReplaceOneAsync(filter, _tracked, cancellationToken: ct);
        return 1;
    }
}
