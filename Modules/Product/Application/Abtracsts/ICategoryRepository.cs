using SharedKernel.Interfaces.Repository;

namespace Modules.Product.Application.Abtracsts;

public interface ICategoryRepository : IRepository<Domain.Category>
{
    Task<Domain.Category?> GetByNameAsync(string name, CancellationToken ct = default);
    Task<List<Domain.Category>> GetAllWithProductsAsync(CancellationToken ct = default);
    Task<Domain.Category?> GetByIdWithProductsAsync(Guid id, CancellationToken ct = default);
    Task<List<Domain.Category>> GetSubCategoriesAsync(Guid parentCategoryId, CancellationToken ct = default);
    Task<Domain.Category?> GetByIdWithSubCategoriesAsync(Guid id, CancellationToken ct = default);
}
