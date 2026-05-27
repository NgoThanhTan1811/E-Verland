using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using Modules.Chat.Domain;

namespace Modules.Chat.Infrastructure.Persistence;

public class ChatMongoDbContext
{
    public IMongoCollection<Conversation> Conversations { get; }
    public IMongoCollection<Message> Messages { get; }

    static ChatMongoDbContext()
    {
        // Register BSON class maps for domain classes with private constructors/setters.
        // We use the private parameterless constructor so MongoDB can set all fields
        // (including Id, CreatedAtUtc) via member maps after construction.
        if (!BsonClassMap.IsClassMapRegistered(typeof(Conversation)))
        {
            BsonClassMap.RegisterClassMap<Conversation>(cm =>
            {
                cm.AutoMap();
                cm.SetIgnoreExtraElements(true);
                // Map the private parameterless constructor so MongoDB can deserialize
                // all stored fields (Id, CreatedAtUtc, LastMessage*, etc.)
                cm.MapConstructor(typeof(Conversation).GetConstructor(
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
                    null, System.Type.EmptyTypes, null)!);
            });
        }

        if (!BsonClassMap.IsClassMapRegistered(typeof(Message)))
        {
            BsonClassMap.RegisterClassMap<Message>(cm =>
            {
                cm.AutoMap();
                cm.SetIgnoreExtraElements(true);
                // Map the private parameterless constructor
                cm.MapConstructor(typeof(Message).GetConstructor(
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
                    null, System.Type.EmptyTypes, null)!);
            });
        }

        // Use standard Guid representation (as string)
        BsonSerializer.TryRegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));
    }

    public ChatMongoDbContext(IMongoClient mongoClient, string databaseName = "chat")
    {
        var db = mongoClient.GetDatabase(databaseName);

        Conversations = db.GetCollection<Conversation>("conversations");
        Messages = db.GetCollection<Message>("messages");

        EnsureIndexesAsync().GetAwaiter().GetResult();
    }

    private async Task EnsureIndexesAsync()
    {
        // Unique index { CustomerId, SellerId } on conversations
        var convIndexKeys = Builders<Conversation>.IndexKeys
            .Ascending(c => c.CustomerId)
            .Ascending(c => c.SellerId);
        var convIndexOptions = new CreateIndexOptions { Unique = true, Name = "unique_customer_seller" };
        await Conversations.Indexes.CreateOneAsync(
            new CreateIndexModel<Conversation>(convIndexKeys, convIndexOptions));

        // Index { ConversationId: 1 } on messages
        var msgIndexKeys = Builders<Message>.IndexKeys.Ascending(m => m.ConversationId);
        var msgIndexOptions = new CreateIndexOptions { Name = "idx_conversation_id" };
        await Messages.Indexes.CreateOneAsync(
            new CreateIndexModel<Message>(msgIndexKeys, msgIndexOptions));
    }
}
