using Microsoft.EntityFrameworkCore;
using Modules.User.Application.Interfaces.Repositories;
using Modules.User.Domain.Entities;
using Modules.User.Infrastructure.Persistence;
using SharedKernel.Pagination;
namespace Modules.User.Infrastructure.Repositories
{
    public class AddressRepository(UserDbContext db) : IAddressRepository
    {
        private readonly UserDbContext _db = db;

        public Task CreateAsync(Address entity, CancellationToken cancellationToken = default)
        {
            return _db.Addresses.AddAsync(entity, cancellationToken).AsTask();
        }

        public async Task<IReadOnlyList<Address>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _db.Addresses.AsNoTracking()
                                     .ToListAsync(cancellationToken);
        }

        public async Task<PageResult<Address>> GetPagedAsync(PagingFilter filter, CancellationToken ct = default)
        {
            var query = _db.Addresses.AsNoTracking().AsQueryable();
            var totalItems = await query.CountAsync(ct);

            var (page, limit, skip) = filter.Normalize();

            var items = await query
                .OrderBy(a => a.CreatedAt)
                .Skip(skip)
                .Take(limit)
                .ToListAsync(ct);

            return new PageResult<Address>
            {
                Items = items,
                TotalItems = totalItems,
                Page = page,
                Limit = limit
            };
        }

        public async Task<Address?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _db.Addresses.AsNoTracking()
                                    .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        }
        // Get address by id and profile id
        public async Task<Address?> GetByIdForProfileAsync(Guid addressId, Guid profileId, CancellationToken ct = default)
        {
            return await _db.Addresses.AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == addressId && a.ProfileId == profileId, ct);

        }

        // Get all addresses by profile id
        public async Task<IReadOnlyList<Address>> GetByProfileIdAsync(Guid profileId, CancellationToken ct = default)
        {
            return await _db.Addresses
                        .AsNoTracking()
                        .OrderByDescending(a => a.IsDefault)
                        .Where(a => a.ProfileId == profileId)
                        .ToListAsync(ct);
        }

        public async Task<Address?> GetDefaultAsync(Guid profileId, CancellationToken ct = default)
        {
            return await _db.Addresses.AsNoTracking()
                .FirstOrDefaultAsync(a => a.ProfileId == profileId && a.IsDefault, ct);
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var existingAddress = await _db.Addresses
                .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

            if (existingAddress == null) return false;

            _db.Entry(existingAddress).Property(nameof(Domain.Entities.Address.IsDeleted)).CurrentValue = true;
            _db.Entry(existingAddress).Property(nameof(Domain.Entities.Address.DeletedAt)).CurrentValue = DateTime.UtcNow;

            return true;
        }

        public Task UnsetDefaultAsync(Guid profileId, CancellationToken ct = default)
        {
            return _db.Database.ExecuteSqlRawAsync(
                """
                UPDATE "Addresses" 
                SET "IsDefault" = FALSE 
                WHERE "ProfileId" = {0} 
                AND "IsDefault" = TRUE
                AND "DeletedAt" IS NULL
                """,
                [profileId],
                ct);
        }

        public Task UpdateAsync(Address entity, CancellationToken cancellationToken = default)
        {
            _db.Addresses.Update(entity);
            return Task.CompletedTask;
        }
    }
}