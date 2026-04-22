using Microsoft.EntityFrameworkCore;
using Modules.Order.Application.Contracts;
using Modules.Order.Infrastructure.Persistence;
using SharedKernel.Pagination;

namespace Modules.Order.Infrastructure.Repositories
{
    public class OrderRepository(OrderDbContext db) : IOrderRepository
    {
        private readonly OrderDbContext _db = db;

        public async Task<bool> CodeExistsAsync(string code, CancellationToken ct = default)
        {
            return await _db.Orders.AnyAsync(o => o.Code == code, ct);
        }

        public async Task CreateAsync(Domain.Order entity, CancellationToken cancellationToken = default)
        {
            await _db.Orders.AddAsync(entity, cancellationToken);
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
            if (order == null) return false;

            _db.Entry(order).Property(nameof(Domain.Order.IsDeleted)).CurrentValue = true;
            _db.Entry(order).Property(nameof(Domain.Order.DeletedAt)).CurrentValue = DateTime.UtcNow;


            return true;
        }

        public async Task<bool> ExistsAsync(Guid orderId, CancellationToken ct = default)
        {
            return await _db.Orders.AnyAsync(o => o.Id == orderId, ct);
        }

        public async Task<Domain.Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _db.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
        }

        public async Task<bool> IsOwnedByUserAsync(Guid orderId, Guid userId, CancellationToken ct = default)
        {
            return await _db.Orders.AnyAsync(o => o.Id == orderId && o.UserId == userId, ct);
        }

        public Task UpdateAsync(Domain.Order entity, CancellationToken cancellationToken = default)
        {
            _db.Orders.Update(entity);
            return Task.CompletedTask;
        }

        public async Task<PageResult<Domain.Order>> GetUserOrdersAsync(Guid userId, PagingFilter filter, CancellationToken ct = default)
        {
            var query = _db.Orders
                .AsNoTracking()
                .Where(o => o.UserId == userId);

            var totalItems = await query.CountAsync(ct);

            var (page, limit, skip) = filter.Normalize();

            var items = await query
                .OrderByDescending(o => o.CreatedAt)
                .Skip(skip)
                .Take(limit)
                .ToListAsync(ct);

            return new PageResult<Domain.Order>
            {
                Items = items,
                TotalItems = totalItems,
                Page = page,
                Limit = limit
            };
        }

        public async Task<PageResult<Domain.Order>> GetFilteredOrdersAsync(
            Guid? userId,
            Domain.OrderStatus? status,
            Domain.PaymentStatus? paymentStatus,
            Domain.PaymentMethod? paymentMethod,
            DateTime? fromDate,
            DateTime? toDate,
            PagingFilter filter,
            CancellationToken ct = default)
        {
            var query = _db.Orders.AsNoTracking();

            // Apply filters
            if (userId.HasValue && userId != Guid.Empty)
                query = query.Where(o => o.UserId == userId.Value);

            if (status.HasValue)
                query = query.Where(o => o.Status == status.Value);

            if (paymentStatus.HasValue)
                query = query.Where(o => o.PaymentStatus == paymentStatus.Value);

            if (paymentMethod.HasValue)
                query = query.Where(o => o.PaymentMethod == paymentMethod.Value);

            if (fromDate.HasValue)
                query = query.Where(o => o.CreatedAt >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(o => o.CreatedAt <= toDate.Value);

            var totalItems = await query.CountAsync(ct);

            var (page, limit, skip) = filter.Normalize();

            var items = await query
                .OrderByDescending(o => o.CreatedAt)
                .Skip(skip)
                .Take(limit)
                .ToListAsync(ct);

            return new PageResult<Domain.Order>
            {
                Items = items,
                TotalItems = totalItems,
                Page = page,
                Limit = limit
            };
        }
    }
}