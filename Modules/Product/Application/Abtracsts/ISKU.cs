using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SharedKernel.Interfaces.Repository;

namespace Modules.Product.Application.Abtracsts
{
    public interface ISKU : IRepository<Domain.SKU>
    {
        Task<Domain.SKU> GetByValueAsync(string value);
        Task<List<Domain.SKU>> GetAllWithProductAsync();
        Task<Domain.SKU> GetByIdWithProductAsync(Guid id);
        
    }
}