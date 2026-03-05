using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Modules.Chat.Infrastructure.Persistence;

public sealed class ChatDbContextFactory : IDesignTimeDbContextFactory<ChatDbContext>
{
    public ChatDbContext CreateDbContext(string[] args)
    {
        var conn =
            Environment.GetEnvironmentVariable("ConnectionStrings__ChatDb")
            ?? throw new InvalidOperationException(
                "Missing env ConnectionStrings__ChatDb (design-time).");

        var options = new DbContextOptionsBuilder<ChatDbContext>()
            .UseNpgsql(conn, npgsql =>
            {
                npgsql.MigrationsAssembly(typeof(ChatDbContext).Assembly.GetName().Name);
            })
            .Options;

        return new ChatDbContext(options);
    }
}