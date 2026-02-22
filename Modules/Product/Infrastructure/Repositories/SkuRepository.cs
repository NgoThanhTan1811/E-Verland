using Microsoft.EntityFrameworkCore;
using Modules.Product.Application.Contracts;
using Modules.Product.Domain;
using Modules.Product.Infrastructure.Persistence;

namespace Modules.Product.Infrastructure.Repositories;

public class SkuRepository : ISkuRepository
{
    private readonly ProductDbContext _db;

    public SkuRepository(ProductDbContext db) => _db = db;

    public Task CreateAsync(SKU entity, CancellationToken cancellationToken = default)
    {
        return _db.SKUs.AddAsync(entity, cancellationToken).AsTask();
    }

    public async Task<SKU?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _db.SKUs.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public Task UpdateAsync(SKU entity, CancellationToken cancellationToken = default)
    {
        _db.SKUs.Update(entity);
        return Task.CompletedTask;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var existing = await _db.SKUs.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (existing == null) return false;

        _db.SKUs.Remove(existing);
        return true;
    }

    public async Task<SKU?> GetByCodeAsync(string value, CancellationToken ct = default)
    {
        var normalized = value.Trim().ToUpperInvariant();
        return await _db.SKUs.AsNoTracking()
            .FirstOrDefaultAsync(s => s.SkuCode.Equals(normalized, StringComparison.CurrentCultureIgnoreCase), ct);
    }

    public async Task<List<SKU>> GetAllWithProductAsync(CancellationToken ct = default)
    {
        return await _db.SKUs
            .Include(s => s.Product)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<SKU?> GetByIdWithProductAsync(Guid id, CancellationToken ct = default)
    {
        return await _db.SKUs
            .Include(s => s.Product)
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id, ct);
    }
}
