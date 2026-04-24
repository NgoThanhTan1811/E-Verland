using Microsoft.EntityFrameworkCore;
using Modules.Product.Application.Contracts;
using Modules.Product.Domain;
using Modules.Product.Infrastructure.Persistence;

namespace Modules.Product.Infrastructure.Services;

public class ProductReservationService(ProductDbContext db) : IProductReservationService
{
    public async Task ReserveStockAsync(
        Guid orderId,
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

        const int maxRetries = 3;
        var attempt = 0;

        while (true)
        {
            attempt++;

            try
            {
                foreach (var (skuId, quantity) in itemList)
                {
                    if (quantity <= 0)
                        throw new InvalidOperationException($"Invalid reserve quantity for SKU {skuId}");

                    var sku = await db.SKUs
                        .Include(x => x.Product)
                        .FirstOrDefaultAsync(x => x.Id == skuId, ct)
                        ?? throw new InvalidOperationException($"SKU {skuId} not found");

                    if (sku.Stock < quantity)
                        throw new InvalidOperationException($"Insufficient stock for SKU {skuId}");

                    sku.Stock -= quantity;

                    if (sku.Stock == 0)
                    {
                        sku.IsActive = false;
                        await UpdateProductStatusFromSkusAsync(sku.ProductId, ct);
                    }

                    db.StockReservations.Add(new StockReservation
                    {
                        OrderId = orderId,
                        PaymentId = paymentId,
                        SkuId = skuId,
                        Quantity = quantity,
                        ReservedAt = DateTime.UtcNow,
                        ExpiresAt = DateTime.UtcNow.AddMinutes(15),
                        Status = ReservationStatus.Reserved
                    });
                }

                await db.SaveChangesAsync(ct);
                return;
            }
            catch (DbUpdateConcurrencyException) when (attempt < maxRetries)
            {
                foreach (var entry in db.ChangeTracker.Entries())
                {
                    entry.State = EntityState.Detached;
                }
            }
        }
    }

    public async Task ConfirmReservationAsync(Guid paymentId, CancellationToken ct = default)
    {
        var reservations = await db.StockReservations
            .Where(r => r.PaymentId == paymentId &&
                        (r.Status == ReservationStatus.Reserved || r.Status == ReservationStatus.Expired))
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
            var sku = await db.SKUs.Include(x => x.Product).FirstOrDefaultAsync(x => x.Id == reservation.SkuId, ct);
            if (sku is not null)
            {
                sku.Stock += reservation.Quantity;
                if (sku.Stock > 0)
                    sku.IsActive = true;

                await UpdateProductStatusFromSkusAsync(sku.ProductId, ct);
            }

            reservation.Status = reservation.Status == ReservationStatus.Expired
                ? ReservationStatus.Expired
                : ReservationStatus.Released;
        }

        await db.SaveChangesAsync(ct);
    }

    private async Task UpdateProductStatusFromSkusAsync(Guid productId, CancellationToken ct)
    {
        var product = await db.Products.Include(x => x.SKUs).FirstOrDefaultAsync(x => x.Id == productId, ct);
        if (product is null)
            return;

        var hasAnyAvailableSku = product.SKUs.Any(s => s.IsActive && s.Stock > 0);
        product.Status = hasAnyAvailableSku
            ? (product.Status == ProductStatus.Draft ? ProductStatus.Draft : ProductStatus.Published)
            : ProductStatus.OutOfStock;
    }
}
