using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Modules.Payment.Application.Contracts;

namespace Modules.Payment.Infrastructure.Services;

public class SellerPayoutBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<SellerPayoutBackgroundService> logger) : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly ILogger<SellerPayoutBackgroundService> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var sellerBalanceService = scope.ServiceProvider.GetRequiredService<ISellerBalanceService>();
                    var released = await sellerBalanceService.ProcessDuePayoutsAsync(stoppingToken);

                    if (released > 0)
                    {
                        _logger.LogInformation("Released {ReleasedCount} pending seller balances to available.", released);
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Failed to process due seller payouts.");
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        catch (ObjectDisposedException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }
}
