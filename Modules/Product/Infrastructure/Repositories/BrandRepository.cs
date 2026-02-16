using Microsoft.EntityFrameworkCore;
using Modules.Product.Application.Abtracsts;
using Modules.Product.Domain;
using Modules.Product.Infrastructure.Perhesistences;

namespace Modules.Product.Infrastructure.Repositories;

public class BrandRepository : IBrandRepository
{
    private readonly ProductDbContext _db;

    public BrandRepository(ProductDbContext db) => _db = db;

    public Task CreateAsync(Brand entity, CancellationToken cancellationToken = default)
    {
        return _db.Brands.AddAsync(entity, cancellationToken).AsTask();
    }

    public async Task<Brand?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _db.Brands.AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
    }

    public Task UpdateAsync(Brand entity, CancellationToken cancellationToken = default)
    {
        _db.Brands.Update(entity);
        return Task.CompletedTask;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var existing = await _db.Brands.FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
        if (existing == null) return false;

        _db.Brands.Remove(existing);
        return true;
    }

    public async Task<Brand?> GetByNameAsync(string name, CancellationToken ct = default)
    {
        var normalized = name.Trim().ToUpperInvariant();
        return await _db.Brands.AsNoTracking()
            .FirstOrDefaultAsync(b => b.Name.Equals(normalized, StringComparison.CurrentCultureIgnoreCase), ct);
    }

    public async Task<List<Brand>> GetAllWithProductsAsync(CancellationToken ct = default)
    {
        return await _db.Brands
            .Include(b => b.Products)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<Brand?> GetByIdWithProductsAsync(Guid id, CancellationToken ct = default)
    {
        return await _db.Brands
            .Include(b => b.Products)
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == id, ct);
    }

}
