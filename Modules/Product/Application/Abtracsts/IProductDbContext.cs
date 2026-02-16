namespace Modules.Product.Application.Abtracsts;

public interface IProductDbContext
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
