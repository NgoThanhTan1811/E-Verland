using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Modules.Product.Infrastructure.Persistence;
using SharedKernel.Persistence;

namespace Modules.Cart.Infrastructure.Persistence;

public sealed class CartDbContextFactory : IDesignTimeDbContextFactory<CartDbContext>
{
    public CartDbContext CreateDbContext(string[] args)
    {
        var conn =
            Environment.GetEnvironmentVariable("CartDb")
            ?? throw new InvalidOperationException(
                "Missing env CartDb (design-time).");

        var optionsBuilder = new DbContextOptionsBuilder<CartDbContext>();
        optionsBuilder.ConfigureNpgsql(conn, typeof(CartDbContext).Assembly.GetName().Name!);
        var options = optionsBuilder.Options;

        return new CartDbContext(options);
    }
}
