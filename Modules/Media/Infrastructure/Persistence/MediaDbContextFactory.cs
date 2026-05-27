using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using SharedKernel.Persistence;

namespace Modules.Media.Infrastructure.Persistence
{
    public class MediaDbContextFactory : IDesignTimeDbContextFactory<MediaDbContext>
    {
        public MediaDbContext CreateDbContext(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .AddUserSecrets<MediaDbContextFactory>() // Nạp User Secrets của máy local vào đây
                .AddEnvironmentVariables()
                .Build();

            var conn = configuration["ConnectionStrings:MediaDb"]
                ?? throw new InvalidOperationException(
                    "Missing env MediaDb (design-time).");

            var optionsBuilder = new DbContextOptionsBuilder<MediaDbContext>();
            optionsBuilder.ConfigureNpgsql(conn, typeof(MediaDbContext).Assembly.GetName().Name!);

            return new MediaDbContext(optionsBuilder.Options);
        }
    }
}