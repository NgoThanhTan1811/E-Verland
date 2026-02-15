
using SharedKernel.Interfaces.Repository;

namespace Modules.Product.Application.Abtracsts
{
    public interface IProduct : IRepository<Domain.Product>
    {
        Task<IReadOnlyList<Domain.Product>> GetAllProductsAsync();
        Task<IEnumerable<Domain.Product>> GetProductsByBrandIdAsync(Guid brandId);
        Task<IEnumerable<Domain.Product>> GetSearchProductsAsync(string search);
    
        
    }
}