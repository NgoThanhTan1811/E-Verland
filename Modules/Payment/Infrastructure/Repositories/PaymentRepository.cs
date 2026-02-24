using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Modules.Payment.Application.Contracts;
using Modules.Payment.Infrastructure.Persistence;

namespace Modules.Payment.Infrastructure.Repositories
{
    public class PaymentRepository(PaymentDbContext dbContext) : IPaymentRepository
    {
        private readonly PaymentDbContext _dbContext = dbContext;

        public async Task CreateAsync(Domain.Payment entity, CancellationToken cancellationToken = default)
        {
            await _dbContext.Payments.AddAsync(entity, cancellationToken);
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var payment = await _dbContext.Payments.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
            if (payment == null) return false;
            _dbContext.Payments.Remove(payment);
            return true;
        }

        public async Task<Domain.Payment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _dbContext.Payments.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        }

        public async Task<Domain.Payment?> GetByOrderIdAsync(Guid orderId, CancellationToken ct = default)
        {
            return await _dbContext.Payments
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.OrderId == orderId, ct);
        }

        public async Task<Domain.Payment?> GetByPaymentCode(string Code, CancellationToken ct = default)
        {
            return await _dbContext.Payments
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Code == Code, ct);
        }

        public async Task<List<Domain.Payment>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
        {
            return await _dbContext.Payments
                    .AsNoTracking()
                    .OrderByDescending(p => p.CreatedAt)
                    .Where(p => p.UserId == userId).ToListAsync(ct);
        }

        public Task<bool> IsPaymentCodeExistsAsync(string code, CancellationToken ct = default)
        {
            return _dbContext.Payments
                    .AsNoTracking()
                    .AnyAsync(p => p.Code == code, ct);
        }

        public Task UpdateAsync(Domain.Payment entity, CancellationToken cancellationToken = default)
        {
            _dbContext.Payments.Update(entity);
            return Task.CompletedTask;
        }
    }
}