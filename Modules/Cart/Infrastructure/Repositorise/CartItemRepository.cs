using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Modules.Cart.Application.Contracts;
using Modules.Cart.Domain;
using Modules.Cart.Infrastructure.Persistence;

namespace Modules.Cart.Infrastructure.Repositorise
{
    public class CartItemRepository(CartDbContext db) : ICartItemRepository
    {
        private readonly CartDbContext _db = db;

        public Task CreateAsync(CartItem entity, CancellationToken cancellationToken = default)
        {
            return _db.CartItems.AddAsync(entity, cancellationToken).AsTask();
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var existing = await _db.CartItems.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
            if (existing == null) return false;
            _db.CartItems.Remove(existing);
            return true;
        }

        public Task<CartItem?> GetByCartIdAndSkuIdAsync(Guid cartId, Guid skuId, CancellationToken ct = default)
        {
            return _db.CartItems.AsNoTracking()
                .FirstOrDefaultAsync(c => c.CartId == cartId && c.SkuId == skuId, ct);
        }

        public Task<List<CartItem>> GetByCartIdAsync(Guid cartId, CancellationToken ct = default)
        {
            return _db.CartItems.AsNoTracking()
                .Where(c => c.CartId == cartId)
                .ToListAsync(ct);
        }

        public Task<CartItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return _db.CartItems.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        }

        public Task UpdateAsync(CartItem entity, CancellationToken cancellationToken = default)
        {
            var trackedEntity = _db.CartItems.Local.FirstOrDefault(e => e.Id == entity.Id);

            if (trackedEntity != null)
            {
                _db.Entry(trackedEntity).CurrentValues.SetValues(entity);
            }
            else
            {
                // Nếu chưa, thực hiện Update bình thường
                _db.CartItems.Update(entity);
            }

            return Task.CompletedTask;
        }
    }
}