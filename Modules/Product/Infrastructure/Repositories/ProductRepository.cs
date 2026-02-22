using Microsoft.EntityFrameworkCore;
using Modules.Product.Application.Contracts;
using Modules.Product.Application.DTOs.Request;
using Modules.Product.Domain;
using Modules.Product.Infrastructure.Persistence;

namespace Modules.Product.Infrastructure.Repositories;

public class ProductRepository(ProductDbContext db) : IProductRepository
{
    private readonly ProductDbContext _db = db;

    public Task CreateAsync(Domain.Product entity, CancellationToken cancellationToken = default)
    {
        return _db.Products.AddAsync(entity, cancellationToken).AsTask();
    }

    public async Task<Domain.Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _db.Products.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public Task UpdateAsync(Domain.Product entity, CancellationToken cancellationToken = default)
    {
        _db.Products.Update(entity);
        return Task.CompletedTask;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var existing = await _db.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (existing == null) return false;

        _db.Products.Remove(existing);
        return true;
    }

    public async Task<IEnumerable<Domain.Product>> GetSearchProductsAdminAsync(FilterProductAdminRequestDto filter, CancellationToken ct = default)
    {
        var query = BuildProductQuery();
        query = ApplyKeywordFilter(query, filter.Keyword);
        query = ApplyBrandFilter(query, filter.BrandId);
        query = ApplyCategoryFilter(query, filter.CategoryId);
        query = ApplyStatusFilter(query, filter.Status);
        query = ApplyPriceFilter(query, filter.MinPrice, filter.MaxPrice, useVirtualPrice: false);
        query = ApplySort(query, filter.SortBy);
        query = ApplyPaging(query, filter.Page, filter.Limit);

        return await query.ToListAsync(ct);
    }

    public async Task<IEnumerable<Domain.Product>> GetSearchProductsCustomerAsync(FilterProductCustomerRequestDto filter, CancellationToken ct = default)
    {
        var query = BuildProductQuery();
        query = ApplyKeywordFilter(query, filter.Keyword);
        query = ApplyBrandFilter(query, filter.BrandId);
        query = ApplyCategoryFilter(query, filter.CategoryId);
        query = ApplyStatusFilter(query, ProductStatus.Active);
        query = ApplyPriceFilter(query, filter.MinPrice, filter.MaxPrice, useVirtualPrice: true);
        query = ApplySort(query, "newest");
        query = ApplyPaging(query, filter.Page, filter.Limit);

        return await query.ToListAsync(ct);
    }

    public Task<bool> IsActiveProductAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        return _db.Products
            .AsNoTracking()
            .AnyAsync(p =>
                p.Id == productId &&
                p.Status == ProductStatus.Active, cancellationToken);
    }

    public Task<int> CountProductsAsync(CancellationToken ct = default)
    {
        return _db.Products.AsNoTracking().CountAsync(ct);
    }


    public async Task<Domain.Product> ChangeStatusAsync(Guid productId, ProductStatus newStatus, CancellationToken cancellationToken = default)
    {
        var product = await _db.Products
            .FirstOrDefaultAsync(p => p.Id == productId, cancellationToken)
                ?? throw new KeyNotFoundException("Product not found");

        product.Status = newStatus;

        return product;
    }

    private IQueryable<Domain.Product> BuildProductQuery()
    {
        return _db.Products
            .Include(p => p.Brand)
            .Include(p => p.Categories)
            .Include(p => p.SKUs)
            .AsNoTracking()
            .AsQueryable();
    }

    private static IQueryable<Domain.Product> ApplyKeywordFilter(IQueryable<Domain.Product> query, string? keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            return query;
        }

        var normalized = keyword.Trim().ToUpperInvariant();
        return query.Where(p =>
            p.Name.ToUpper().Contains(normalized) ||
            p.Description.ToUpper().Contains(normalized));
    }

    private static IQueryable<Domain.Product> ApplyBrandFilter(IQueryable<Domain.Product> query, Guid? brandId)
    {
        return brandId.HasValue
            ? query.Where(p => p.BrandId == brandId)
            : query;
    }

    private static IQueryable<Domain.Product> ApplyCategoryFilter(IQueryable<Domain.Product> query, Guid? categoryId)
    {
        return categoryId.HasValue
            ? query.Where(p => p.Categories.Any(c => c.Id == categoryId))
            : query;
    }

    private static IQueryable<Domain.Product> ApplyStatusFilter(IQueryable<Domain.Product> query, ProductStatus? status)
    {
        return status.HasValue
            ? query.Where(p => p.Status == status)
            : query;
    }

    private static IQueryable<Domain.Product> ApplyPriceFilter(
        IQueryable<Domain.Product> query,
        decimal? minPrice,
        decimal? maxPrice,
        bool useVirtualPrice)
    {
        if (minPrice.HasValue)
        {
            query = useVirtualPrice
                ? query.Where(p => p.VirtualPrice >= minPrice)
                : query.Where(p => p.BasePrice >= minPrice);
        }

        if (maxPrice.HasValue)
        {
            query = useVirtualPrice
                ? query.Where(p => p.VirtualPrice <= maxPrice)
                : query.Where(p => p.BasePrice <= maxPrice);
        }

        return query;
    }

    private static IQueryable<Domain.Product> ApplySort(IQueryable<Domain.Product> query, string? sortBy)
    {
        var normalized = sortBy?.Trim().ToLowerInvariant();
        return normalized switch
        {
            "price_asc" => query.OrderBy(p => p.BasePrice),
            "price_desc" => query.OrderByDescending(p => p.BasePrice),
            "name_asc" => query.OrderBy(p => p.Name),
            "name_desc" => query.OrderByDescending(p => p.Name),
            "oldest" => query.OrderBy(p => p.CreatedAt),
            _ => query.OrderByDescending(p => p.CreatedAt)
        };
    }

    private static IQueryable<Domain.Product> ApplyPaging(IQueryable<Domain.Product> query, int page, int limit)
    {
        if (page <= 0 || limit <= 0)
        {
            return query;
        }

        return query.Skip((page - 1) * limit).Take(limit);
    }
}
