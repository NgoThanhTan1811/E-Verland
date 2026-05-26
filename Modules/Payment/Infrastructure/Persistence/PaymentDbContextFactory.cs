using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Modules.Payment.Infrastructure.Persistence;
using SharedKernel.Persistence;

namespace Modules.Payment.Infrastructure.Persistence;

public sealed class PaymentDbContextFactory : IDesignTimeDbContextFactory<PaymentDbContext>
{
    public PaymentDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
           .SetBasePath(Directory.GetCurrentDirectory())
           .AddJsonFile("appsettings.json", optional: true)
           .AddJsonFile("appsettings.Development.json", optional: true)
           .AddUserSecrets<PaymentDbContextFactory>() // Nạp User Secrets của máy local vào đây
           .AddEnvironmentVariables()
           .Build();
           
        var conn =
            configuration["ConnectionStrings:PaymentDb"]
            ?? throw new InvalidOperationException(
                "Missing env PaymentDb (design-time).");

        var optionsBuilder = new DbContextOptionsBuilder<PaymentDbContext>();
        optionsBuilder.ConfigureNpgsql(conn, typeof(PaymentDbContext).Assembly.GetName().Name!);
        var options = optionsBuilder.Options;

        return new PaymentDbContext(options);
    }
}
