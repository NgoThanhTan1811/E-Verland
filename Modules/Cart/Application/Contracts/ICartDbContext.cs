namespace Modules.Cart.Application.Contracts;

public interface ICartDbContext
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
