using Microsoft.EntityFrameworkCore;

namespace SharedKernel.Persistence;

public static class DbContextOptionsExtensions
{
    public static void ConfigureRelationalDefaults(this DbContextOptionsBuilder optionsBuilder, bool readHeavy = false)
    {
        optionsBuilder.EnableSensitiveDataLogging(false);

        if (readHeavy)
        {
            optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTrackingWithIdentityResolution);
        }
    }

    public static void ConfigureNpgsql(this DbContextOptionsBuilder optionsBuilder, string connectionString, string migrationsAssembly, bool readHeavy = false)
    {
        optionsBuilder.UseNpgsql(connectionString, npgsql =>
        {
            npgsql.MigrationsAssembly(migrationsAssembly);
            npgsql.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null);
        });

        optionsBuilder.ConfigureRelationalDefaults(readHeavy);
    }
}
