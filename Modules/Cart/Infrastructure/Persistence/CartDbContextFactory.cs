using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Modules.Product.Infrastructure.Persistence;

namespace Modules.Cart.Infrastructure.Persistence;

public sealed class CartDbContextFactory : IDesignTimeDbContextFactory<CartDbContext>
{
    public CartDbContext CreateDbContext(string[] args)
    {
        var conn =
            Environment.GetEnvironmentVariable("CartDb")
            ?? throw new InvalidOperationException(
                "Missing env CartDb (design-time).");

        var options = new DbContextOptionsBuilder<CartDbContext>()
            .UseNpgsql(conn, npgsql =>
            {
                npgsql.MigrationsAssembly(typeof(CartDbContext).Assembly.GetName().Name);
            })
            .Options;

        return new CartDbContext(options);
    }
}