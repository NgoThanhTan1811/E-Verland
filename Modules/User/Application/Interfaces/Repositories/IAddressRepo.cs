using SharedKernel.Interfaces.Repository;
using Modules.User.Domain.Entities;

namespace Modules.User.Application.Interfaces.Repositories
{
    public interface IAddressRepository : IRepository<Address>
    {
        Task<IReadOnlyCollection<Address>> GetByProfileIdAsync(Guid ProfileId, CancellationToken ct = default);

        Task<Address?> GetDefaultAsync(Guid ProfileId, CancellationToken ct = default);

        Task<Address?> GetByIdForProfileAsync(Guid addressId, Guid ProfileId, CancellationToken ct = default);

        Task UnsetDefaultAsync(Guid ProfileId, CancellationToken ct = default);
    }
}