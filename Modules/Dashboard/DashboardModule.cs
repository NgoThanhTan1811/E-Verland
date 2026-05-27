using MediatR;
using Modules.Dashboard.Application;
using Modules.Dashboard.Application.Contracts;
using Modules.Dashboard.Infrastructure.Options;
using Modules.Dashboard.Infrastructure.Services;

namespace Modules.Dashboard;

public static class DashboardModuleExtension
{
    public static IServiceCollection AddDashboardModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<DashboardOptions>(configuration.GetSection(DashboardOptions.SectionName));

        services.AddScoped<IDashboardMetricsCache, DashboardMetricsCacheService>();
        services.AddHostedService<DashboardMetricsRefreshService>();

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(DashboardApplicationMarker).Assembly));

        return services;
    }
}
