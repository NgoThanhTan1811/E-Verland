using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using SharedKernel.Persistence;
using System.IO;

namespace Modules.Shipping.Infrastructure.Persistence;

public sealed class ShippingDbContextFactory : IDesignTimeDbContextFactory<ShippingDbContext>
{
    public ShippingDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddUserSecrets<ShippingDbContextFactory>() // Nạp User Secrets của máy local vào đây
            .AddEnvironmentVariables()
            .Build();

        var conn = configuration["ConnectionStrings:ShippingDb"]
            ?? throw new InvalidOperationException(
                "Missing env ShippingDb (design-time).");

        var optionsBuilder = new DbContextOptionsBuilder<ShippingDbContext>();
        optionsBuilder.ConfigureNpgsql(conn, typeof(ShippingDbContext).Assembly.GetName().Name!);

        return new ShippingDbContext(optionsBuilder.Options);
    }
}
