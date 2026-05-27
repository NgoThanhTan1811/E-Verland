using Microsoft.Extensions.Options;
using Modules.Dashboard.Application.Contracts;
using Modules.Dashboard.Infrastructure.Options;

namespace Modules.Dashboard.Infrastructure.Services;

public sealed class DashboardMetricsRefreshService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly DashboardOptions _options;
    private readonly ILogger<DashboardMetricsRefreshService> _logger;

    public DashboardMetricsRefreshService(
        IServiceScopeFactory scopeFactory,
        IOptions<DashboardOptions> options,
        ILogger<DashboardMetricsRefreshService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var refreshInterval = TimeSpan.FromMinutes(Math.Max(1, _options.RefreshIntervalMinutes));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var cache = scope.ServiceProvider.GetRequiredService<IDashboardMetricsCache>();
                await cache.RefreshSnapshotsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to refresh dashboard snapshots.");
            }

            await Task.Delay(refreshInterval, stoppingToken);
        }
    }
}
