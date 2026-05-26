using Microsoft.EntityFrameworkCore;
using Modules.Shipping.Application.Contracts;
using Modules.Shipping.Domain;
using Modules.Shipping.Infrastructure.Persistence;

namespace Modules.Shipping.Infrastructure.Repositories;

public sealed class ShippingRepository(ShippingDbContext db) : IShippingRepository
{
    private readonly ShippingDbContext _db = db;

    public async Task<ShippingOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _db.ShippingOrders.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<ShippingOrder?> GetByOrderIdAsync(Guid orderId, CancellationToken ct = default)
    {
        return await _db.ShippingOrders.FirstOrDefaultAsync(s => s.OrderId == orderId, ct);
    }

    public async Task<ShippingOrder?> GetByProviderOrderCodeAsync(string providerOrderCode, CancellationToken ct = default)
    {
        return await _db.ShippingOrders.FirstOrDefaultAsync(s => s.ProviderOrderCode == providerOrderCode, ct);
    }

    public async Task<bool> ExistsForOrderAsync(Guid orderId, CancellationToken ct = default)
    {
        return await _db.ShippingOrders.AnyAsync(s => s.OrderId == orderId, ct);
    }

    public async Task<List<ShippingOrder>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        return await _db.ShippingOrders
            .AsNoTracking()
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task CreateAsync(ShippingOrder entity, CancellationToken cancellationToken = default)
    {
        await _db.ShippingOrders.AddAsync(entity, cancellationToken);
    }

    public Task UpdateAsync(ShippingOrder entity, CancellationToken cancellationToken = default)
    {
        _db.ShippingOrders.Update(entity);
        return Task.CompletedTask;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var shipping = await _db.ShippingOrders.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (shipping == null) return false;

        _db.Entry(shipping).Property(nameof(ShippingOrder.IsDeleted)).CurrentValue = true;
        _db.Entry(shipping).Property(nameof(ShippingOrder.DeletedAt)).CurrentValue = DateTime.UtcNow;

        return true;
    }
}
