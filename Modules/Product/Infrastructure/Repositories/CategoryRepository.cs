using Microsoft.EntityFrameworkCore;
using Modules.Product.Application.Contracts;
using Modules.Product.Application.DTOs.Response;
using Modules.Product.Domain;
using Modules.Product.Infrastructure.Persistence;

namespace Modules.Product.Infrastructure.Repositories;

public class CategoryRepository(ProductDbContext db) : ICategoryRepository
{
    private readonly ProductDbContext _db = db;

    public Task CreateAsync(Category entity, CancellationToken cancellationToken = default)
    {
        return _db.Categories.AddAsync(entity, cancellationToken).AsTask();
    }

    public async Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _db.Categories
            .AsTracking()
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
        return await _db.Categories
            .FirstOrDefaultAsync(c => c.Name.ToUpper() == normalized, ct);
    }

    public async Task<List<CategoryListItemDto>> GetAllWithProductsAsync(CancellationToken ct = default)
    {
        return await _db.Categories
                .Select(c => new CategoryListItemDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    ParentCategoryId = c.ParentCategoryId,
                    CreatedAt = c.CreatedAt
                })
                .ToListAsync(ct);
    }

    public async Task<Category?> GetByIdWithProductsAsync(Guid id, CancellationToken ct = default)
    {
        return await _db.Categories
            .Include(c => c.Products)
            .FirstOrDefaultAsync(c => c.Id == id, ct);
    }

    public async Task<List<Category>> GetSubCategoriesAsync(Guid parentCategoryId, CancellationToken ct = default)
    {
        return await _db.Categories
            .Where(c => c.ParentCategoryId == parentCategoryId)
            .ToListAsync(ct);
    }

    public async Task<Category?> GetByIdWithSubCategoriesAsync(Guid id, CancellationToken ct = default)
    {
        return await _db.Categories
            .Include(c => c.SubCategories)
            .FirstOrDefaultAsync(c => c.Id == id, ct);
    }
}
