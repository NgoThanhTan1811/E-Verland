using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Modules.Product.Infrastructure.Persistence;

namespace Modules.Product.Infrastructure.Persistence;

public sealed class ProductDbContextFactory : IDesignTimeDbContextFactory<ProductDbContext>
{
    public ProductDbContext CreateDbContext(string[] args)
    {
        var conn =
            Environment.GetEnvironmentVariable("ConnectionStrings__ProductDb")
            ?? throw new InvalidOperationException(
                "Missing env ConnectionStrings__ProductDb (design-time).");

        var options = new DbContextOptionsBuilder<ProductDbContext>()
            .UseNpgsql(conn, npgsql =>
            {
                npgsql.MigrationsAssembly(typeof(ProductDbContext).Assembly.GetName().Name);
            })
            .Options;

        return new ProductDbContext(options);
    }
}