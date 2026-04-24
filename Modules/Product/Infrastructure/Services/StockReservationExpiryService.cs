using Microsoft.EntityFrameworkCore;
using Modules.Product.Application.Contracts;
using Modules.Product.Domain;
using Modules.Product.Infrastructure.Persistence;

namespace Modules.Product.Infrastructure.Services;

public class StockReservationExpiryService(
    IServiceScopeFactory scopeFactory,
    ILogger<StockReservationExpiryService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);

            try
            {
                await ProcessExpiredReservationsAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Error occurred while processing expired stock reservations.");
            }
        }
    }

    private async Task ProcessExpiredReservationsAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ProductDbContext>();
        var reservationService = scope.ServiceProvider.GetRequiredService<IProductReservationService>();

        var expiredPaymentIds = await db.StockReservations
            .Where(r => r.Status == ReservationStatus.Reserved && r.ExpiresAt <= DateTime.UtcNow)
            .Select(r => r.PaymentId)
            .Distinct()
            .ToListAsync(ct);

        if (expiredPaymentIds.Count == 0)
            return;

        logger.LogInformation("Found {Count} expired stock reservations to release.", expiredPaymentIds.Count);

        foreach (var paymentId in expiredPaymentIds)
        {
            try
            {
                var pending = await db.StockReservations
                    .Where(r => r.PaymentId == paymentId && r.Status == ReservationStatus.Reserved)
                    .ToListAsync(ct);

                foreach (var item in pending)
                {
                    item.Status = ReservationStatus.Expired;
                }

                await db.SaveChangesAsync(ct);
                await reservationService.ReleaseReservationAsync(paymentId, ct);
                logger.LogInformation("Released stock reservation for PaymentId {PaymentId}.", paymentId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to release stock reservation for PaymentId {PaymentId}.", paymentId);
            }
        }
    }
}
