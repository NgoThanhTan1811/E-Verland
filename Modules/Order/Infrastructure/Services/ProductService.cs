using Microsoft.EntityFrameworkCore;
using Modules.Order.Application.Contracts;
using Modules.Product.Infrastructure.Persistence;

namespace Modules.Order.Infrastructure.Services;

public class ProductService : IProductService
{
    private readonly ProductDbContext _productDbContext;

    public ProductService(ProductDbContext productDbContext)
    {
        _productDbContext = productDbContext;
    }

    public async Task<ProductDto?> GetProductAsync(Guid productId, CancellationToken ct = default)
    {
        var product = await _productDbContext.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == productId, ct);

        if (product == null)
            return null;

        return new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            Price = product.BasePrice
        };
    }
}
