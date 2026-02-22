namespace Modules.Product.Application.Contracts;

public interface IProductDbContext
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
