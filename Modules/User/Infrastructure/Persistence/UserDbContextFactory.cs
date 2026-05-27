using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using SharedKernel.Persistence;
using System.IO;

namespace Modules.User.Infrastructure.Persistence;

public sealed class UserDbContextFactory : IDesignTimeDbContextFactory<UserDbContext>
{
    public UserDbContext CreateDbContext(string[] args )
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddUserSecrets<UserDbContextFactory>() // Nạp User Secrets của máy local vào đây
            .AddEnvironmentVariables()
            .Build();

        var conn = configuration["ConnectionStrings:UserDb"]
            ?? throw new InvalidOperationException(
                "Missing env UserDb (design-time).");

        var optionsBuilder = new DbContextOptionsBuilder<UserDbContext>();
        optionsBuilder.ConfigureNpgsql(conn, typeof(UserDbContext).Assembly.GetName().Name!);
        
        return new UserDbContext(optionsBuilder.Options);
    }
}
