using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SharedKernel.Interfaces.Repository;

namespace Modules.Product.Application.Abtracsts
{
    public interface ICategory : IRepository<Domain.Category>
    {
        Task<Domain.Category> GetByNameAsync(string name);
        Task<List<Domain.Category>> GetAllWithProductsAsync();
        Task<Domain.Category> GetByIdWithProductsAsync(Guid id);
        Task<List<Domain.Category>> GetSubCategoriesAsync(Guid parentCategoryId);
        Task<Domain.Category> GetByIdWithSubCategoriesAsync(Guid id);

    
    }
}