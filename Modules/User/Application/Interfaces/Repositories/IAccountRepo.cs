
using Modules.User.Domain.Entities;
using Modules.User.Application.DTOs.Request;
using SharedKernel.Interfaces.Repository;
using SharedKernel;

namespace Modules.User.Application.Interfaces.Repositories
{
    public interface IAccountRepository : IRepository<Account>
    {

        Task<PageResult<Account>> SearchAsync(AccountFilter filter, CancellationToken ct);

        Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default);
        Task<bool> ExistsByUsernameAsync(string username, CancellationToken ct = default);

        Task<Account?> GetByEmailAsync(string email, CancellationToken ct = default);
        Task<Account?> GetByUsernameAsync(string username, CancellationToken ct = default);
    }

}