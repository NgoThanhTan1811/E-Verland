using Modules.User.Domain.Entities;
using SharedKernel.Interfaces.Repository;

namespace Modules.User.Application.Interfaces.Repositories;

public interface IProfileRepository : IRepository<Profile>
{
    Task<Profile?> GetByAccountIdAsync(Guid accountId, CancellationToken ct = default);

    Task<Profile?> GetWithAddressesAsync(Guid accountId, CancellationToken ct = default);
    Task<Profile?> GetWithBankAccountsAsync(Guid accountId, CancellationToken ct = default);
    Task<Profile?> GetFullAsync(Guid accountId, CancellationToken ct = default);


}