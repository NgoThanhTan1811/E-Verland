using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SharedKernel.Interfaces.Repository;
using SharedKernel.Pagination;
using Modules.Order.Domain;

namespace Modules.Order.Application.Contracts
{
    public interface IOrderRepository : IRepository<Domain.Order>
    {
        Task<bool> ExistsAsync(Guid orderId, CancellationToken ct = default);
        Task<bool> IsOwnedByUserAsync(Guid orderId, Guid userId, CancellationToken ct = default);
        Task<bool> CodeExistsAsync(string code, CancellationToken ct = default);
        Task<PageResult<Domain.Order>> GetUserOrdersAsync(Guid userId, PagingFilter filter, CancellationToken ct = default);
        Task<PageResult<Domain.Order>> GetFilteredOrdersAsync(
            Guid? userId,
            OrderStatus? status,
            PaymentStatus? paymentStatus,
            PaymentMethod? paymentMethod,
            DateTime? fromDate,
            DateTime? toDate,
            PagingFilter filter,
            CancellationToken ct = default
        );
    }
}