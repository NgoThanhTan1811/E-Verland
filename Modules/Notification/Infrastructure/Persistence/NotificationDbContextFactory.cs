using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using SharedKernel.Persistence;

namespace Modules.Notification.Infrastructure.Persistence;

public sealed class NotificationDbContextFactory : IDesignTimeDbContextFactory<NotificationDbContext>
{
    public NotificationDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
           .SetBasePath(Directory.GetCurrentDirectory())
           .AddJsonFile("appsettings.json", optional: true)
           .AddJsonFile("appsettings.Development.json", optional: true)
           .AddUserSecrets<NotificationDbContextFactory>() // Nạp User Secrets của máy local vào đây
           .AddEnvironmentVariables()
           .Build(); 

        var conn =
            configuration["ConnectionStrings:NotificationDb"]
            ?? throw new InvalidOperationException(
                "Missing env NotificationDb (design-time).");

        var optionsBuilder = new DbContextOptionsBuilder<NotificationDbContext>();
        optionsBuilder.ConfigureNpgsql(conn, typeof(NotificationDbContext).Assembly.GetName().Name!, readHeavy: true);
        var options = optionsBuilder.Options;

        return new NotificationDbContext(options);
    }
}
