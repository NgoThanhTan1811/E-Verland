
using SharedKernel.Interfaces.Repository;
using Modules.Product.Application.DTOs.Request;
using Modules.Product.Domain;

namespace Modules.Product.Application.Contracts
{
    public interface IProductRepository : IRepository<Domain.Product>
    {
        Task<IEnumerable<Domain.Product>> GetSearchProductsAdminAsync(FilterProductAdminRequestDto filter, CancellationToken ct = default);
        Task<IEnumerable<Domain.Product>> GetSearchProductsCustomerAsync(FilterProductCustomerRequestDto filter, CancellationToken ct = default);
        Task<Domain.Product?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken cancellationToken = default);
        Task<bool> RestoreAsync(Guid id, CancellationToken cancellationToken = default);
        Task<Domain.Product> ChangeStatusAsync(Guid productId, ProductStatus newStatus, CancellationToken cancellationToken = default);

        Task<bool> IsActiveProductAsync(Guid productId, CancellationToken ct = default);

        Task<int> CountProductsAsync(CancellationToken ct = default);
    }
}
