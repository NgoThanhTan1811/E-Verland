using SharedKernel.Interfaces.Repository;

namespace Modules.Product.Application.Abtracsts;

public interface ISkuRepository : IRepository<Domain.SKU>
{
    Task<Domain.SKU?> GetByCodeAsync(string skuCode, CancellationToken ct = default);
    Task<List<Domain.SKU>> GetAllWithProductAsync(CancellationToken ct = default);
    Task<Domain.SKU?> GetByIdWithProductAsync(Guid id, CancellationToken ct = default);
}
