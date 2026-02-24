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
    public class ProfileRepository(UserDbContext db) : IProfileRepository
    {
        private readonly UserDbContext _db = db;

        public Task CreateAsync(Domain.Entities.Profile entity, CancellationToken cancellationToken = default)
        {
            return _db.Profiles.AddAsync(entity, cancellationToken).AsTask();
        }

        public async Task<IReadOnlyList<Domain.Entities.Profile>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _db.Profiles.AsNoTracking()
                                 .ToListAsync(cancellationToken);
        }

        public async Task<PageResult<Domain.Entities.Profile>> GetPagedAsync(PagingFilter filter, CancellationToken ct = default)
        {
            var query = _db.Profiles.AsNoTracking().AsQueryable();
            var totalItems = await query.CountAsync(ct);

            var (page, limit, skip) = filter.Normalize();

            var items = await query
                .OrderBy(p => p.CreatedAt)
                .Skip(skip)
                .Take(limit)
                .ToListAsync(ct);

            return new PageResult<Domain.Entities.Profile>
            {
                Items = items,
                TotalItems = totalItems,
                Page = page,
                Limit = limit
            };
        }

        public async Task<Domain.Entities.Profile?> GetByAccountIdAsync(Guid accountId, CancellationToken ct = default)
        {
            return await _db.Profiles.AsNoTracking()
                                 .FirstOrDefaultAsync(up => up.AccountId == accountId, ct);
        }

        public async Task<Domain.Entities.Profile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _db.Profiles
                                .Include(p => p.Account)
                                .Include(p => p.Addresses)
                                .Include(p => p.BankAccounts)
                                .FirstOrDefaultAsync(up => up.Id == id, cancellationToken);
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
        {
            var existingProfile = await _db.Profiles
                .FirstOrDefaultAsync(up => up.Id == id, ct);

            if (existingProfile == null) return false;

            _db.Profiles.Remove(existingProfile);
            return true;
        }


        public Task UpdateAsync(Domain.Entities.Profile entity, CancellationToken cancellationToken = default)
        {
            _db.Profiles.Update(entity);
            return Task.CompletedTask;
        }
    }
}