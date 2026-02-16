using SharedKernel.Interfaces.Repository;

namespace Modules.Product.Application.Abtracsts;

public interface IBrandRepository : IRepository<Domain.Brand>
{
    Task<Domain.Brand?> GetByNameAsync(string name, CancellationToken ct = default);
    Task<List<Domain.Brand>> GetAllWithProductsAsync(CancellationToken ct = default);
    Task<Domain.Brand?> GetByIdWithProductsAsync(Guid id, CancellationToken ct = default);
}
