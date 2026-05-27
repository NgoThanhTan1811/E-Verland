using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Modules.Product.Infrastructure.Persistence;
using SharedKernel.Persistence;

namespace Modules.Product.Infrastructure.Persistence;

public sealed class ProductDbContextFactory : IDesignTimeDbContextFactory<ProductDbContext>
{
    public ProductDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddUserSecrets<ProductDbContextFactory>() // Nạp User Secrets của máy local vào đây
            .AddEnvironmentVariables()
            .Build();

        var conn = configuration["ConnectionStrings:ProductDb"]

            ?? throw new InvalidOperationException(
                "Missing env ProductDb (design-time).");

        var optionsBuilder = new DbContextOptionsBuilder<ProductDbContext>();
        optionsBuilder.ConfigureNpgsql(conn, typeof(ProductDbContext).Assembly.GetName().Name!, readHeavy: true);
        var options = optionsBuilder.Options;

        return new ProductDbContext(options);
    }
}
