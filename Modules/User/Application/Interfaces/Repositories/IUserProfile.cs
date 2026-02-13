using Modules.User.Domain.Entities;
using SharedKernel.Interfaces.Repository;

namespace Modules.User.Application.Interfaces.Repositories;

public interface IProfileRepository : IRepository<Profile>
{
    Task<IReadOnlyList<Profile>> GetAllAsync(CancellationToken ct = default);
    Task<Profile?> GetByAccountIdAsync(Guid accountId, CancellationToken ct = default);

}