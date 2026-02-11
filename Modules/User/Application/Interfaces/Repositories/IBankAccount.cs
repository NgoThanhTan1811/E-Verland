using Modules.User.Domain.Entities;
using SharedKernel.Interfaces.Repository;
namespace Modules.User.Application.Interfaces.Repositories
{
    public interface IBankAccountRepository : IRepository<BankAccount>
    {
        Task<IReadOnlyCollection<BankAccount>> GetByProfileIdAsync(Guid ProfileId, CancellationToken ct = default);

        Task<BankAccount?> GetByIdForProfileAsync(Guid bankAccountId, Guid ProfileId, CancellationToken ct = default);

        Task<bool> ExistsAccountNumberAsync(Guid ProfileId, string accountNumber, CancellationToken ct = default);
    }
}