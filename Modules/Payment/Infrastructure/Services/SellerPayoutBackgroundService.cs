using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Modules.Payment.Application.Contracts;

namespace Modules.Payment.Infrastructure.Services;

public class SellerPayoutBackgroundService(
    IServiceProvider serviceProvider,
    ILogger<SellerPayoutBackgroundService> logger) : BackgroundService
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly ILogger<SellerPayoutBackgroundService> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var sellerBalanceService = scope.ServiceProvider.GetRequiredService<ISellerBalanceService>();
                var released = await sellerBalanceService.ProcessDuePayoutsAsync(stoppingToken);

                if (released > 0)
                {
                    _logger.LogInformation("Released {ReleasedCount} pending seller balances to available.", released);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process due seller payouts.");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
