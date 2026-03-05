using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Modules.Notification.Infrastructure.Persistence;

public sealed class NotificationDbContextFactory : IDesignTimeDbContextFactory<NotificationDbContext>
{
    public NotificationDbContext CreateDbContext(string[] args)
    {
        var conn =
            Environment.GetEnvironmentVariable("ConnectionStrings__NotificationDb")
            ?? throw new InvalidOperationException(
                "Missing env ConnectionStrings__NotificationDb (design-time).");

        var options = new DbContextOptionsBuilder<NotificationDbContext>()
            .UseNpgsql(conn, npgsql =>
            {
                npgsql.MigrationsAssembly(typeof(NotificationDbContext).Assembly.GetName().Name);
            })
            .Options;

        return new NotificationDbContext(options);
    }
}