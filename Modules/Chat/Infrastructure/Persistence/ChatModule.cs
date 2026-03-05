using Microsoft.EntityFrameworkCore;
using Modules.Chat.Application.Contracts;
using Modules.Chat.Infrastructure.Repository;

namespace Modules.Chat.Infrastructure.Persistence;

public static class ChatModuleExtensions
{
    public static IServiceCollection AddChatModule(this IServiceCollection services, IConfiguration configuration)
    {
        // Add DbContext
        var conn = configuration.GetConnectionString("ChatDb")
            ?? throw new InvalidOperationException("Missing ConnectionStrings:ChatDb");

        services.AddDbContext<ChatDbContext>(options =>
            options.UseNpgsql(conn, npgsql =>
            {
                npgsql.MigrationsAssembly(typeof(ChatDbContext).Assembly.GetName().Name);
            }));

        // Add Repositories
        services.AddScoped<IConversationRepository, ConversationRepository>();
        services.AddScoped<IMessageRepository, MessageRepository>();

        // Add MediatR
        services.AddMediatR(config =>
            config.RegisterServicesFromAssembly(typeof(Application.ChatApplicationMarker).Assembly));

        // Add AutoMapper
        services.AddAutoMapper(typeof(Application.ChatApplicationMarker).Assembly);

        return services;
    }
}
