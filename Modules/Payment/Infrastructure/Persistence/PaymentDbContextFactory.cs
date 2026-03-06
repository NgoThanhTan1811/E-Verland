using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Modules.Payment.Infrastructure.Persistence;

namespace Modules.Payment.Infrastructure.Persistence;

public sealed class PaymentDbContextFactory : IDesignTimeDbContextFactory<PaymentDbContext>
{
    public PaymentDbContext CreateDbContext(string[] args)
    {
        var conn =
            Environment.GetEnvironmentVariable("PaymentDb")
            ?? throw new InvalidOperationException(
                "Missing env PaymentDb (design-time).");

        var options = new DbContextOptionsBuilder<PaymentDbContext>()
            .UseNpgsql(conn, npgsql =>
            {
                npgsql.MigrationsAssembly(typeof(PaymentDbContext).Assembly.GetName().Name);
            })
            .Options;

        return new PaymentDbContext(options);
    }
}