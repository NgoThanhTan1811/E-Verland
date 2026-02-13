using Modules.User.Domain.Entities;
using SharedKernel.Interfaces.Repository;
namespace Modules.User.Application.Interfaces.Repositories
{
    public interface IBankAccountRepository : IRepository<BankAccount>
    {   
        Task <IReadOnlyList<BankAccount>> GetAllAsync(CancellationToken ct = default);
        Task<IReadOnlyList<BankAccount>> GetByProfileIdAsync(Guid ProfileId, CancellationToken ct = default);

        Task<BankAccount?> GetByIdForProfileAsync(Guid bankAccountId, Guid ProfileId, CancellationToken ct = default);

        Task<bool> ExistsAccountNumberAsync(Guid ProfileId, string accountNumber, Guid excludeBankAccountId, CancellationToken ct = default);
        Task<bool> DeleteBankAccountAsync(Guid BankAccountId, Guid ProfileId, CancellationToken cancellationToken = default);
    }
}