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

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupPendingUploadsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed during orphan media cleanup.");
            }

            await Task.Delay(TimeSpan.FromHours(intervalHours), stoppingToken);
        }
    }

    private async Task CleanupPendingUploadsAsync(CancellationToken ct)
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
            if (await storage.ExistsAsync(media.FilePath, ct))
            {
                await storage.DeleteAsync(media.FilePath, ct);
            }

            media.Status = MediaFileStatus.Orphan;
            await repository.DeleteAsync(media.Id, ct);
        }

        _logger.LogInformation("Orphan media cleanup completed. Removed {Count} stale pending uploads.", stalePending.Count);
    }
}
