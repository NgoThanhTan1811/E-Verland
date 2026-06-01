using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Modules.Media.Application.Interfaces;
using Modules.Media.Domain;
using Modules.Media.Infrastructure.Options;
using SharedKernel.Entities;

namespace Modules.Media.Infrastructure.Services;

public sealed class OrphanMediaCleanupService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OrphanMediaCleanupService> _logger;
    private readonly MediaOptions _options;

    public OrphanMediaCleanupService(
        IServiceScopeFactory scopeFactory,
        ILogger<OrphanMediaCleanupService> logger,
        IOptions<MediaOptions> options)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalHours = Math.Max(1, _options.CleanupIntervalHours);
        using var timer = new PeriodicTimer(TimeSpan.FromHours(intervalHours));

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await CleanupPendingUploadsAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Expected during host shutdown.
        }
        catch (TaskCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Expected during host shutdown.
        }
    }

    private async Task CleanupPendingUploadsAsync(CancellationToken ct)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IMediaFileRepository>();
            var storage = scope.ServiceProvider.GetRequiredService<IMediaStorageService>();

            var graceHours = Math.Max(1, _options.OrphanGracePeriodHours);
            var threshold = DateTime.UtcNow.AddHours(-graceHours);

            var stalePending = await repository.GetPendingOlderThanAsync(threshold, ct);
            if (stalePending.Count == 0)
            {
                return;
            }

            foreach (var media in stalePending)
            {
                if (ct.IsCancellationRequested)
                {
                    return;
                }

                if (await storage.ExistsAsync(media.FilePath, ct))
                {
                    await storage.DeleteAsync(media.FilePath, ct);
                }

                media.Status = MediaFileStatus.Orphan;
                await repository.DeleteAsync(media.Id, ct);
            }

            _logger.LogInformation("Orphan media cleanup completed. Removed {Count} stale pending uploads.", stalePending.Count);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // Expected during host shutdown.
        }
        catch (TaskCanceledException) when (ct.IsCancellationRequested)
        {
            // Expected during host shutdown.
        }
        catch (ObjectDisposedException) when (ct.IsCancellationRequested)
        {
            // Expected when the host is already shutting down.
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed during orphan media cleanup.");
        }
    }
}
