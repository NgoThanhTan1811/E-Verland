using Microsoft.EntityFrameworkCore;
using Modules.User.Infrastructure.Persistence;
using Modules.User.Domain.Entities;
using Modules.User.Application.Interfaces.Repositories;
namespace Modules.User.Infrastructure.Repositories
{
    public class AddressRepository : IAddressRepository
    {
        private readonly UserDbContext _db;
        public AddressRepository(UserDbContext db) => _db = db;
        public Task CreateAsync(Address entity, CancellationToken cancellationToken = default)
        {
            return _db.Addresses.AddAsync(entity, cancellationToken).AsTask();
        }

        public async Task<IReadOnlyCollection<Address>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _db.Addresses.AsNoTracking()
                                     .ToListAsync(cancellationToken);
        }

        public async Task<Address?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _db.Addresses.AsNoTracking()
                                    .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        }

        public async Task<Address?> GetByIdForProfileAsync(Guid addressId, Guid ProfileId, CancellationToken ct = default)
        {
            return await _db.Addresses.AsNoTracking()
                                    .FirstOrDefaultAsync(a => a.Id == addressId && a.ProfileId == ProfileId, ct);
        }

        public async Task<IReadOnlyCollection<Address>> GetByProfileIdAsync(Guid ProfileId, CancellationToken ct = default)
        {
            return await _db.Addresses.AsNoTracking()
                .Where(a => a.ProfileId == ProfileId)
                .ToListAsync(ct);
        }

        public async Task<Address?> GetDefaultAsync(Guid ProfileId, CancellationToken ct = default)
        {
            return await _db.Addresses.AsNoTracking()
                .FirstOrDefaultAsync(a => a.ProfileId == ProfileId && a.IsDefault, ct);
        }

        public async Task<bool> DeleteAsync(Guid id, Guid byId, CancellationToken cancellationToken = default)
        {
            var existingAddress = await _db.Addresses
                .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

            if (existingAddress == null) return false;

            return true;
        }

        public Task UnsetDefaultAsync(Guid ProfileId, CancellationToken ct = default)
        {
            return _db.Database.ExecuteSqlRawAsync(
                """
                UPDATE "Addresses" 
                SET "IsDefault" = FALSE 
                WHERE "ProfileId" = {0} 
                AND "IsDefault" = TRUE
                AND "DeletedAt" IS NULL
                """,
                [ProfileId], ct);
        }

        public Task UpdateAsync(Address entity, CancellationToken cancellationToken = default)
        {
            _db.Addresses.Update(entity);
            return Task.CompletedTask;
        }
    }
}