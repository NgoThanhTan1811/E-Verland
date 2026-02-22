
using SharedKernel.Interfaces.Repository;
using Modules.Product.Application.DTOs.Request;

namespace Modules.Product.Application.Contracts
{
    public interface IProductRepository : IRepository<Domain.Product>
    {
        Task<IEnumerable<Domain.Product>> GetSearchProductsAdminAsync(FilterProductAdminRequestDto filter, CancellationToken ct = default);
        Task<IEnumerable<Domain.Product>> GetSearchProductsCustomerAsync(FilterProductCustomerRequestDto filter, CancellationToken ct = default);

        Task<bool> IsActiveProductAsync(Guid productId, CancellationToken ct = default);

        Task<int> CountProductsAsync(CancellationToken ct = default);
    }
}