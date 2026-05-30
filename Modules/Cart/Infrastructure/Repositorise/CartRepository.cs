using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Modules.Cart.Application.Contracts;
using Modules.Cart.Infrastructure.Persistence;

namespace Modules.Cart.Infrastructure.Repositorise
{
    public class CartRepository(CartDbContext db) : ICartRepository
    {
        private readonly CartDbContext _db = db;

        public Task CreateAsync(Domain.Cart entity, CancellationToken cancellationToken = default)
        {
            return _db.Carts.AddAsync(entity, cancellationToken).AsTask();
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var existing = await _db.Carts.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
            if (existing == null) return false;
            _db.Carts.Remove(existing);
            return true;
        }

        public Task<Domain.Cart?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return _db.Carts.AsNoTracking()
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        }

        public Task<Domain.Cart?> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
        {
            return _db.Carts.AsNoTracking()
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.UserId == userId, ct);
        }

        public async Task UpdateAsync(Domain.Cart entity, CancellationToken cancellationToken = default)
        {
            var trackedEntity = _db.Carts.Local.FirstOrDefault(e => e.Id == entity.Id);

            if (trackedEntity != null)
            {
                _db.Entry(trackedEntity).CurrentValues.SetValues(entity);
            }
            else
            {
                trackedEntity = await _db.Carts.FirstOrDefaultAsync(c => c.Id == entity.Id, cancellationToken);

                if (trackedEntity != null)
                {
                    _db.Entry(trackedEntity).CurrentValues.SetValues(entity);
                }
            }

        }
    }
}