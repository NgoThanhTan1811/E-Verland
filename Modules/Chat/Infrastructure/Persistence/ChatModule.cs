using MongoDB.Driver;
using Modules.Chat.Application.Contracts;
using Modules.Chat.Infrastructure.Repository;

namespace Modules.Chat.Infrastructure.Persistence;

public static class ChatModuleExtension
{
    public static IServiceCollection AddChatModule(this IServiceCollection services, IConfiguration configuration)
    {

        var host = configuration["MongoDB:Host"];
        var user = configuration["MongoDB:User"];
        var pass = configuration["MongoDB:Password"];
        var appName = configuration["MongoDB:AppName"];

        var connectionString = $"mongodb+srv://{user}:{pass}@{host}/?appName={appName}"
             ?? throw new InvalidOperationException("Missing ConnectionStrings:MongoChatDb connection string.");

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
