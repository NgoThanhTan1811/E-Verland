using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Storage;

namespace Modules.Order.Application.Contracts
{
    public interface IOrderDbContext
    {
        Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken ct);


        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}