
using Modules.Cart.Domain;
using SharedKernel.Interfaces.Repository;

namespace Modules.Cart.Application.Contracts
{
    public interface ICartItemRepository : IRepository<CartItem>
    {
        Task<CartItem?> GetByCartIdAndProductIdAsync(Guid cartId, Guid skuId, CancellationToken ct = default);
        Task<List<CartItem>> GetByCartIdAsync(Guid cartId, CancellationToken ct = default);
    }
}