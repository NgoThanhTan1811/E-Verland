namespace Modules.Order.Application.Contracts;

public interface IOrderPaymentSyncService
{
    Task SyncPaymentAsync(Guid orderId, Guid paymentId, string paymentStatus, CancellationToken ct = default);
}
