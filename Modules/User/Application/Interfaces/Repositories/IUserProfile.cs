using Modules.User.Domain.Entities;
using SharedKernel.Pagination;
using SharedKernel.Interfaces.Repository;

namespace Modules.User.Application.Interfaces.Repositories;

public interface IProfileRepository : IRepository<Profile>
{
    Task<PageResult<Profile>> GetPagedAsync(PagingFilter filter, CancellationToken ct = default);
    Task<IReadOnlyList<Profile>> GetAllAsync(CancellationToken ct = default);
    Task<Profile?> GetByAccountIdAsync(Guid accountId, CancellationToken ct = default);

}