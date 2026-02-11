using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Modules.User.Application.Interfaces.Repositories;
using Modules.User.Infrastructure.Persistence;

namespace Modules.User.Infrastructure.Repositories
{
    public class BankAccountRepository : IBankAccountRepository

    {
        private readonly UserDbContext _db;
        public BankAccountRepository(UserDbContext db) => _db = db;
        public Task CreateAsync(Domain.Entities.BankAccount entity, CancellationToken cancellationToken = default)
        {
            return _db.BankAccounts.AddAsync(entity, cancellationToken).AsTask();
        }

        public async Task<bool> ExistsAccountNumberAsync(Guid ProfileId, string accountNumber, CancellationToken ct = default)
        {
            return await _db.BankAccounts.AnyAsync(ba => ba.ProfileId == ProfileId && ba.AccountNumber == accountNumber, ct);

        }

        public async Task<IReadOnlyCollection<Domain.Entities.BankAccount>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _db.BankAccounts.AsNoTracking()
                                 .ToListAsync(cancellationToken);
        }

        public Task<Domain.Entities.BankAccount?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return _db.BankAccounts.AsNoTracking()
                                 .FirstOrDefaultAsync(ba => ba.Id == id, cancellationToken);
        }

        public Task<Domain.Entities.BankAccount?> GetByIdForProfileAsync(Guid bankAccountId, Guid ProfileId, CancellationToken ct = default)
        {
            return _db.BankAccounts.AsNoTracking()
                                 .FirstOrDefaultAsync(ba => ba.Id == bankAccountId && ba.ProfileId == ProfileId, ct);
        }

        public async Task<IReadOnlyCollection<Domain.Entities.BankAccount>> GetByProfileIdAsync(Guid ProfileId, CancellationToken ct = default)
        {
            return await _db.BankAccounts.AsNoTracking()
                .Where(ba => ba.ProfileId == ProfileId)
                .ToListAsync(ct);
        }

        public async Task<bool> DeleteAsync(Guid id, Guid byId, CancellationToken cancellationToken = default)
        {
            var existingBankAccount = await _db.BankAccounts
                 .FirstOrDefaultAsync(ba => ba.Id == id, cancellationToken);

            if (existingBankAccount == null) return false;

            return true;
        }

        public Task UpdateAsync(Domain.Entities.BankAccount entity, CancellationToken cancellationToken = default)
        {
            _db.BankAccounts.Update(entity);
            return Task.CompletedTask;
        }
    }
}