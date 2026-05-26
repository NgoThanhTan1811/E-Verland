using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Modules.Product.Infrastructure.Persistence;
using SharedKernel.Persistence;

namespace Modules.Cart.Infrastructure.Persistence;

public sealed class CartDbContextFactory : IDesignTimeDbContextFactory<CartDbContext>
{
    public CartDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
           .SetBasePath(Directory.GetCurrentDirectory())
           .AddJsonFile("appsettings.json", optional: true)
           .AddJsonFile("appsettings.Development.json", optional: true)
           .AddUserSecrets<CartDbContextFactory>() 
           .AddEnvironmentVariables()
           .Build();

        var conn =
            configuration["ConnectionStrings:CartDb"]
            ?? throw new InvalidOperationException(
                "Missing env CartDb (design-time).");

        var optionsBuilder = new DbContextOptionsBuilder<CartDbContext>();
        optionsBuilder.ConfigureNpgsql(conn, typeof(CartDbContext).Assembly.GetName().Name!);
        return new CartDbContext(optionsBuilder.Options);
    }
}
