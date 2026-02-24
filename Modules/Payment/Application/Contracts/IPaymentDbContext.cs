using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Modules.Payment.Application.Contracts
{
    public interface IPaymentDbContext
    {
      Task<int> SaveChangesAsync(CancellationToken cancellationToken);   
    }
}