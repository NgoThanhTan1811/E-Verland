using Microsoft.EntityFrameworkCore;
using Modules.Product.Application.Abtracsts;
using Modules.Product.Domain;
using Modules.Product.Infrastructure.Perhesistences;

namespace Modules.Product.Infrastructure.Repositories;

public class CategoryRepository : ICategoryRepository
{
    private readonly ProductDbContext _db;

    public CategoryRepository(ProductDbContext db) => _db = db;

    public Task CreateAsync(Category entity, CancellationToken cancellationToken = default)
    {
        return _db.Categories.AddAsync(entity, cancellationToken).AsTask();
    }

    public async Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _db.Categories.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public Task UpdateAsync(Category entity, CancellationToken cancellationToken = default)
    {
        _db.Categories.Update(entity);
        return Task.CompletedTask;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var existing = await _db.Categories.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (existing == null) return false;

        _db.Categories.Remove(existing);
        return true;
    }

    public async Task<Category?> GetByNameAsync(string name, CancellationToken ct = default)
    {
        var normalized = name.Trim().ToUpperInvariant();
        return await _db.Categories.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Name.Equals(normalized, StringComparison.CurrentCultureIgnoreCase), ct);
    }

    public async Task<List<Category>> GetAllWithProductsAsync(CancellationToken ct = default)
    {
        return await _db.Categories
            .Include(c => c.Products)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<Category?> GetByIdWithProductsAsync(Guid id, CancellationToken ct = default)
    {
        return await _db.Categories
            .Include(c => c.Products)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, ct);
    }

    public async Task<List<Category>> GetSubCategoriesAsync(Guid parentCategoryId, CancellationToken ct = default)
    {
        return await _db.Categories.AsNoTracking()
            .Where(c => c.ParentCategoryId == parentCategoryId)
            .ToListAsync(ct);
    }

    public async Task<Category?> GetByIdWithSubCategoriesAsync(Guid id, CancellationToken ct = default)
    {
        return await _db.Categories
            .Include(c => c.SubCategories)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, ct);
    }
}
