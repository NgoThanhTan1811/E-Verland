using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using SharedKernel.Persistence;

namespace Modules.Order.Infrastructure.Persistence;

public sealed class OrderDbContextFactory : IDesignTimeDbContextFactory<OrderDbContext>
{
    public OrderDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddUserSecrets<OrderDbContextFactory>() // Nạp User Secrets của máy local vào đây
            .AddEnvironmentVariables()
            .Build();

        var conn =
            configuration["ConnectionStrings:OrderDb"]
            ?? throw new InvalidOperationException(
                "Missing env OrderDb (design-time).");

        var optionsBuilder = new DbContextOptionsBuilder<OrderDbContext>();
        optionsBuilder.ConfigureNpgsql(conn, typeof(OrderDbContext).Assembly.GetName().Name!, readHeavy: true);
        var options = optionsBuilder.Options;

        return new OrderDbContext(options);
    }
}
