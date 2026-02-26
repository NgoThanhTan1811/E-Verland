using Microsoft.EntityFrameworkCore;
using Modules.Notification.Application.Contracts;
using Modules.Notification.Infrastructure.Repository;
using Modules.Notification.Infrastructure.Persistence;
using Modules.Notification.Infrastructure.Services;

namespace Modules.Notification.Infrastructure;

public static class NotificationModuleExtensions
{
    public static IServiceCollection AddNotificationModule(this IServiceCollection services, IConfiguration configuration)
    {
        // Add DbContext
        var conn = configuration.GetConnectionString("NotificationDb")
            ?? throw new InvalidOperationException("Missing ConnectionStrings:NotificationDb");

        services.AddDbContext<NotificationDbContext>(options =>
            options.UseNpgsql(conn, npgsql =>
            {
                npgsql.MigrationsAssembly(typeof(NotificationDbContext).Assembly.GetName().Name);
            }));

        // Add Repositories
        services.AddScoped<INotificationRepository, NotificationRepository>();

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
