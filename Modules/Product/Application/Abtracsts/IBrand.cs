using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SharedKernel.Interfaces.Repository;

namespace Modules.Product.Application.Abtracsts
{
    public interface IBrand : IRepository<Domain.Brand>
    {
        Task<Domain.Brand> GetByNameAsync(string name);
        Task<List<Domain.Brand>> GetAllWithProductsAsync();
        Task<Domain.Brand> GetByIdWithProductsAsync(Guid id);
        
    }
}