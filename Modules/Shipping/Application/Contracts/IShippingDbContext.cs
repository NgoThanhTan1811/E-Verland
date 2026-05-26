using Microsoft.EntityFrameworkCore.Storage;

namespace Modules.Shipping.Application.Contracts;

public interface IShippingDbContext
{
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken ct);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
