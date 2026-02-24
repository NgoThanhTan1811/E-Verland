using SharedKernel.Interfaces.Repository;

namespace Modules.Payment.Application.Contracts
{
    public interface IPaymentRepository : IRepository<Domain.Payment>
    {
        public Task<Domain.Payment?> GetByOrderIdAsync(Guid orderId, CancellationToken ct = default);
        public Task<Domain.Payment?> GetByPaymentCode(string Code, CancellationToken ct = default);
        public Task<List<Domain.Payment>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
        public Task<bool> IsPaymentCodeExistsAsync(string code, CancellationToken ct = default);
    }
}