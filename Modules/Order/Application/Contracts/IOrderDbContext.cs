using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Modules.Order.Application.Contracts
{
    public interface IOrderDbContext
    {


        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}