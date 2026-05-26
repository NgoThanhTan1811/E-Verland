using MediatR;
using Microsoft.EntityFrameworkCore;
using Modules.Media.Application;
using Modules.Media.Application.Interfaces;
using Modules.Media.Infrastructure.Options;
using Modules.Media.Infrastructure.Repositories;
using Modules.Media.Infrastructure.Services;
using SharedKernel.Persistence;

namespace Modules.Media.Infrastructure.Persistence;

public static class MediaModule
{
    public static IServiceCollection AddMediaModule(this IServiceCollection services, IConfiguration configuration)
    {
        var conn = configuration["ConnectionStrings:MediaDb"]
            ?? throw new InvalidOperationException("Missing ConnectionStrings:MediaDb");

        services.AddDbContext<MediaDbContext>(options =>
            options.ConfigureNpgsql(conn, typeof(MediaDbContext).Assembly.GetName().Name!, readHeavy: true));

        services.Configure<MediaOptions>(configuration.GetSection(MediaOptions.SectionName));
        services.AddHttpClient();

        services.AddScoped<IMediaFileRepository, MediaFileRepository>();
        services.AddScoped<IMediaStorageService, MediaStorageService>();
        services.AddHostedService<OrphanMediaCleanupService>();

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(MediaApplicationMarker).Assembly));

        return services;
    }
}
