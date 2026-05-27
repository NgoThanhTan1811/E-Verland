using Modules.Shipping.Domain;
using SharedKernel.Interfaces.Repository;

namespace Modules.Shipping.Application.Contracts;

public interface IShippingRepository : IRepository<ShippingOrder>
{
    Task<ShippingOrder?> GetByOrderIdAsync(Guid orderId, CancellationToken ct = default);

    Task<ShippingOrder?> GetByProviderOrderCodeAsync(string providerOrderCode, CancellationToken ct = default);

    Task<bool> ExistsForOrderAsync(Guid orderId, CancellationToken ct = default);

    Task<List<ShippingOrder>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
}
