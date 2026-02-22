using SharedKernel.Interfaces.Repository;

namespace Modules.Cart.Application.Contracts
{
    public interface ICartRepository : IRepository<Domain.Cart>
    {
        Task<Domain.Cart?> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    }
}