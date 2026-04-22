using MongoDB.Driver;
using Modules.Chat.Application.Contracts;
using Modules.Chat.Infrastructure.Repository;

namespace Modules.Chat.Infrastructure.Persistence;

public static class ChatModuleExtension
{
    public static IServiceCollection AddChatModule(this IServiceCollection services, IConfiguration configuration)
    {
        // Read MongoDB connection string
        var connectionString =
            configuration["MongoDB:ChatConnectionString"]
            ?? Environment.GetEnvironmentVariable("ChatDb")
            ?? throw new InvalidOperationException(
                "Missing MongoDB connection string. Set 'MongoDB:ChatConnectionString' in configuration or 'MONGODB_CHAT_CONNECTION_STRING' environment variable.");

        // Register IMongoClient as singleton
        services.AddSingleton<IMongoClient>(_ => new MongoClient(connectionString));

        // Register ChatMongoDbContext as singleton
        services.AddSingleton<ChatMongoDbContext>();

        // Register repositories
        services.AddScoped<IConversationRepository, MongoConversationRepository>();
        services.AddScoped<IMessageRepository, MongoMessageRepository>();

        // Add MediatR
        services.AddMediatR(config =>
            config.RegisterServicesFromAssembly(typeof(Application.ChatApplicationMarker).Assembly));

        // Add AutoMapper
        services.AddAutoMapper(typeof(Application.ChatApplicationMarker).Assembly);

        // Add SignalR
        services.AddSignalR();

        return services;
    }
}
