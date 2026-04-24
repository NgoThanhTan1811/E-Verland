using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Modules.User.Application.Interfaces.Repositories;
using Modules.User.Infrastructure.Persistence;
using SharedKernel.Pagination;

namespace Modules.User.Infrastructure.Repositories
{
    public class BankAccountRepository(UserDbContext db) : IBankAccountRepository

    {
        private readonly UserDbContext _db = db;

        public Task CreateAsync(Domain.Entities.BankAccount entity, CancellationToken cancellationToken = default)
        {
            return _db.BankAccounts.AddAsync(entity, cancellationToken).AsTask();
        }

        public async Task<bool> ExistsAccountNumberAsync(Guid ProfileId, string accountNumber, Guid excludeBankAccountId, CancellationToken ct = default)
        {
            return await _db.BankAccounts.AnyAsync(ba => ba.ProfileId == ProfileId && ba.AccountNumber == accountNumber && ba.Id != excludeBankAccountId, ct);

        }

        public async Task<IReadOnlyList<Domain.Entities.BankAccount>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _db.BankAccounts.AsNoTracking()
                                 .ToListAsync(cancellationToken);
        }

        public async Task<PageResult<Domain.Entities.BankAccount>> GetPagedAsync(PagingFilter filter, CancellationToken ct = default)
        {
            var query = _db.BankAccounts.AsNoTracking().AsQueryable();
            var totalItems = await query.CountAsync(ct);

            var (page, limit, skip) = filter.Normalize();

            var items = await query
                .OrderBy(ba => ba.CreatedAt)
                .Skip(skip)
                .Take(limit)
                .ToListAsync(ct);

            return new PageResult<Domain.Entities.BankAccount>
            {
                Items = items,
                TotalItems = totalItems,
                Page = page,
                Limit = limit
            };
        }

        public async Task<Domain.Entities.BankAccount?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _db.BankAccounts.AsNoTracking()
                                 .FirstOrDefaultAsync(ba => ba.Id == id, cancellationToken);
        }
        // Get bank account by id and profile id
        public async Task<Domain.Entities.BankAccount?> GetByIdForProfileAsync(Guid bankAccountId, Guid ProfileId, CancellationToken ct = default)
        {
            return await _db.BankAccounts.AsNoTracking()
                                 .FirstOrDefaultAsync(ba => ba.Id == bankAccountId && ba.ProfileId == ProfileId, ct);
        }
        // Get bank accounts by profile id
        public async Task<IReadOnlyList<Domain.Entities.BankAccount>> GetByProfileIdAsync(Guid ProfileId, CancellationToken ct = default)
        {
            return await _db.BankAccounts.AsNoTracking()
                .Where(ba => ba.ProfileId == ProfileId)
                .ToListAsync(ct);
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var existingBankAccount = await _db.BankAccounts
                 .FirstOrDefaultAsync(ba => ba.Id == id, cancellationToken);

            if (existingBankAccount == null) return false;

            _db.Entry(existingBankAccount).Property(nameof(Domain.Entities.BankAccount.IsDeleted)).CurrentValue = true;
            _db.Entry(existingBankAccount).Property(nameof(Domain.Entities.BankAccount.DeletedAt)).CurrentValue = DateTime.UtcNow;

            return true;
        }

        public Task UpdateAsync(Domain.Entities.BankAccount entity, CancellationToken cancellationToken = default)
        {
            _db.BankAccounts.Update(entity);
            return Task.CompletedTask;
        }

        public async Task<bool> DeleteBankAccountAsync(Guid bankAccountId, Guid profileId, CancellationToken ct)
        {
            var now = DateTime.UtcNow;

            var affected = await _db.BankAccounts
                .Where(x => x.Id == bankAccountId && x.ProfileId == profileId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.IsDeleted, true)
                    .SetProperty(x => x.DeletedAt, now), ct);

            return affected > 0;
        }

    }
}