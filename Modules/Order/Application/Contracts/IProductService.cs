namespace Modules.Order.Application.Contracts;

public interface IProductService
{
    Task<ProductDto?> GetProductAsync(Guid productId, CancellationToken ct = default);
}

public class ProductDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public decimal Price { get; set; }
}
