using SharedKernel.Interfaces.Repository;
using Modules.User.Domain.Entities;

namespace Modules.User.Application.Interfaces.Repositories
{
    public interface IAddressRepository : IRepository<Address>
    {
        Task<IReadOnlyList<Address>> GetAllAsync(CancellationToken ct = default);
        Task<IReadOnlyList<Address>> GetByProfileIdAsync(Guid profileId, CancellationToken ct = default);

        Task<Address?> GetDefaultAsync(Guid profileId, CancellationToken ct = default);
        Task<Address?> GetByIdForProfileAsync(Guid addressId, Guid profileId, CancellationToken ct = default);

        Task UnsetDefaultAsync(Guid profileId, CancellationToken ct = default);
    }
}