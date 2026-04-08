using Microsoft.EntityFrameworkCore;
using Modules.Product.Application.Contracts;
using Modules.Product.Domain;
using Modules.Product.Infrastructure.Persistence;

namespace Modules.Product.Infrastructure.Services;

public class ProductReservationService(ProductDbContext db) : IProductReservationService
{
    public async Task ReserveStockAsync(
        Guid paymentId,
        IEnumerable<(Guid SkuId, int Quantity)> items,
        CancellationToken ct = default)
    {
        // Idempotency: if reservations already exist for this paymentId, do nothing
        var alreadyReserved = await db.StockReservations
            .AnyAsync(r => r.PaymentId == paymentId, ct);

        if (alreadyReserved)
            return;

        var itemList = items.ToList();

        foreach (var (skuId, quantity) in itemList)
        {
            var sku = await db.SKUs.FindAsync([skuId], ct)
                ?? throw new InvalidOperationException($"SKU {skuId} not found");

            if (sku.Stock < quantity)
                throw new InvalidOperationException($"Insufficient stock for SKU {skuId}");

            sku.Stock -= quantity;

            db.StockReservations.Add(new StockReservation
            {
                PaymentId = paymentId,
                SkuId = skuId,
                Quantity = quantity,
                Status = ReservationStatus.Reserved
            });
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task ConfirmReservationAsync(Guid paymentId, CancellationToken ct = default)
    {
        var reservations = await db.StockReservations
            .Where(r => r.PaymentId == paymentId && r.Status == ReservationStatus.Reserved)
            .ToListAsync(ct);

        foreach (var reservation in reservations)
            reservation.Status = ReservationStatus.Confirmed;

        await db.SaveChangesAsync(ct);
    }

    public async Task ReleaseReservationAsync(Guid paymentId, CancellationToken ct = default)
    {
        var reservations = await db.StockReservations
            .Where(r => r.PaymentId == paymentId && r.Status == ReservationStatus.Reserved)
            .ToListAsync(ct);

        foreach (var reservation in reservations)
        {
            var sku = await db.SKUs.FindAsync([reservation.SkuId], ct);
            if (sku is not null)
                sku.Stock += reservation.Quantity;

            reservation.Status = ReservationStatus.Released;
        }

        await db.SaveChangesAsync(ct);
    }
}
