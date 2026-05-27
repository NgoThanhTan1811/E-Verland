using Modules.Notification.Application.Contracts;
using Modules.Notification.Infrastructure.Repository;
using Modules.Notification.Infrastructure.Services;

namespace Modules.Notification.Infrastructure;

public static class NotificationModuleExtension
{
    public static IServiceCollection AddNotificationModule(this IServiceCollection services, IConfiguration configuration)
    {
        // IConnectionMultiplexer is registered by AddRedisModule — no new Redis connection needed.
        // ISNSService is registered by AddAWSInfrastructure — no re-registration needed.

        // Add Repositories
        services.AddScoped<INotificationRepository, RedisNotificationRepository>();

        // Add Singleton for real-time SSE connections
        services.AddSingleton<INotificationService, NotificationService>();

        // Add MediatR
        services.AddMediatR(config =>
            config.RegisterServicesFromAssembly(typeof(Notification.Application.NotificationApplicationMarker).Assembly));

        // Add AutoMapper
        services.AddAutoMapper(typeof(Notification.Application.NotificationApplicationMarker).Assembly);

        return services;
    }
}
