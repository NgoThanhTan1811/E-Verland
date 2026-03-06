using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Modules.User.Infrastructure.Persistence;

public sealed class UserDbContextFactory : IDesignTimeDbContextFactory<UserDbContext>
{
    public UserDbContext CreateDbContext(string[] args)
    {
        var conn =
            Environment.GetEnvironmentVariable("UserDb")
            ?? throw new InvalidOperationException(
                "Missing env UserDb (design-time).");

        var options = new DbContextOptionsBuilder<UserDbContext>()
            .UseNpgsql(conn, npgsql =>
            {
                npgsql.MigrationsAssembly(typeof(UserDbContext).Assembly.GetName().Name);
            })
            .Options;

        return new UserDbContext(options);
    }
}