using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using SharedKernel.Persistence;

namespace Modules.Notification.Infrastructure.Persistence;

public sealed class NotificationDbContextFactory : IDesignTimeDbContextFactory<NotificationDbContext>
{
    public NotificationDbContext CreateDbContext(string[] args)
    {
        var conn =
            Environment.GetEnvironmentVariable("NotificationDb")
            ?? throw new InvalidOperationException(
                "Missing env NotificationDb (design-time).");

        var optionsBuilder = new DbContextOptionsBuilder<NotificationDbContext>();
        optionsBuilder.ConfigureNpgsql(conn, typeof(NotificationDbContext).Assembly.GetName().Name!, readHeavy: true);
        var options = optionsBuilder.Options;

        return new NotificationDbContext(options);
    }
}
