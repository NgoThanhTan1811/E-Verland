using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Modules.Order.Infrastructure.Persistence;

public sealed class OrderDbContextFactory : IDesignTimeDbContextFactory<OrderDbContext>
{
    public OrderDbContext CreateDbContext(string[] args)
    {
        var conn =
            Environment.GetEnvironmentVariable("OrderDb")
            ?? throw new InvalidOperationException(
                "Missing env OrderDb (design-time).");

        var options = new DbContextOptionsBuilder<OrderDbContext>()
            .UseNpgsql(conn, npgsql =>
            {
                npgsql.MigrationsAssembly(typeof(OrderDbContext).Assembly.GetName().Name);
            })
            .Options;

        return new OrderDbContext(options);
    }
}