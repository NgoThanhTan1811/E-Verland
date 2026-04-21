namespace Modules.Payment.Application.Contracts;

public interface ISellerBalanceService
{
    Task EnsurePendingBalanceAsync(
        Guid orderId,
        Guid sellerId,
        decimal amount,
        string currency,
        DateTime availableAtUtc,
        CancellationToken ct = default);

    Task<int> ProcessDuePayoutsAsync(CancellationToken ct = default);
}
