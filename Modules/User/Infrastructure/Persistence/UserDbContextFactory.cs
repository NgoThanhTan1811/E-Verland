using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using SharedKernel.Persistence;

namespace Modules.User.Infrastructure.Persistence;

public sealed class UserDbContextFactory : IDesignTimeDbContextFactory<UserDbContext>
{
    public UserDbContext CreateDbContext(string[] args)
    {
        var conn =
            Environment.GetEnvironmentVariable("UserDb")
            ?? throw new InvalidOperationException(
                "Missing env UserDb (design-time).");

        var optionsBuilder = new DbContextOptionsBuilder<UserDbContext>();
        optionsBuilder.ConfigureNpgsql(conn, typeof(UserDbContext).Assembly.GetName().Name!);
        var options = optionsBuilder.Options;

        return new UserDbContext(options);
    }
}
