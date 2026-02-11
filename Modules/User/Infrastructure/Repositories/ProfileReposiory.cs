using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Modules.User.Application.Interfaces.Repositories;
using Modules.User.Infrastructure.Persistence;

namespace Modules.User.Infrastructure.Repositories
{
    public class ProfileRepository : IProfileRepository
    {
        private readonly UserDbContext _db;
        public ProfileRepository(UserDbContext db) => _db = db;
        public Task CreateAsync(Domain.Entities.Profile entity, CancellationToken cancellationToken = default)
        {
            return _db.Profiles.AddAsync(entity, cancellationToken).AsTask();
        }

        public async Task<IReadOnlyCollection<Domain.Entities.Profile>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _db.Profiles.AsNoTracking()
                                 .ToListAsync(cancellationToken);
        }

        public async Task<Domain.Entities.Profile?> GetByAccountIdAsync(Guid accountId, CancellationToken ct = default)
        {
            return await _db.Profiles.AsNoTracking()
                                 .FirstOrDefaultAsync(up => up.AccountId == accountId, ct);
        }

        public async Task<Domain.Entities.Profile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _db.Profiles.AsNoTracking()
                                 .FirstOrDefaultAsync(up => up.Id == id, cancellationToken);
        }

        public async Task<Domain.Entities.Profile?> GetFullAsync(Guid accountId, CancellationToken ct = default)
        {
            return await _db.Profiles.AsNoTracking()
                                 .Include(up => up.Addresses)
                                 .Include(up => up.BankAccounts)
                                 .FirstOrDefaultAsync(up => up.AccountId == accountId, ct);
        }

        public async Task<Domain.Entities.Profile?> GetWithAddressesAsync(Guid accountId, CancellationToken ct = default)
        {
            return await _db.Profiles.AsNoTracking()
                                 .Include(up => up.Addresses)
                                 .FirstOrDefaultAsync(up => up.AccountId == accountId, ct);
        }

        public async Task<Domain.Entities.Profile?> GetWithBankAccountsAsync(Guid accountId, CancellationToken ct = default)
        {
            return await _db.Profiles.AsNoTracking()
                                 .Include(up => up.BankAccounts)
                                 .FirstOrDefaultAsync(up => up.AccountId == accountId, ct);
        }

        public async Task<bool> DeleteAsync(Guid id, Guid byId, CancellationToken ct = default)
        {
            var existingProfile = await _db.Profiles
                .FirstOrDefaultAsync(up => up.Id == id, ct);

            if (existingProfile == null) return false;

            return true;
        }


        public Task UpdateAsync(Domain.Entities.Profile entity, CancellationToken cancellationToken = default)
        {
            _db.Profiles.Update(entity);
            return Task.CompletedTask;
        }
    }
}