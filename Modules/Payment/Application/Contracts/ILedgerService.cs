using Modules.Payment.Domain;

namespace Modules.Payment.Application.Contracts;

public sealed record LedgerEntryReadModel(
    Guid TransactionId,
    Guid OrderId,
    string? PayoutId,
    LedgerEntryType EntryType,
    LedgerAccountType AccountType,
    decimal Amount,
    string Currency,
    DateTime TimestampUtc,
    string CreatedBy,
    LedgerTransactionStatus Status
);

public interface ILedgerService
{
    Task<bool> RecordIncomingPaymentAsync(
        Guid orderId,
        decimal amount,
        string currency,
        string idempotencyKey,
        string createdBy,
        CancellationToken ct = default);

    Task<bool> RecordSellerPayoutAsync(
        Guid orderId,
        string payoutId,
        decimal amount,
        string currency,
        string idempotencyKey,
        string createdBy,
        CancellationToken ct = default);

    Task<IReadOnlyList<LedgerEntryReadModel>> QueryEntriesAsync(
        Guid? orderId,
        string? payoutId,
        LedgerAccountType? accountType,
        DateTime? fromUtc,
        DateTime? toUtc,
        CancellationToken ct = default);
}
