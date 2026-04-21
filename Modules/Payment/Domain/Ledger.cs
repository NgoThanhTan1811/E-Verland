using SharedKernel.Entities;

namespace Modules.Payment.Domain;

public enum LedgerEntryType
{
    Debit = 1,
    Credit = 2
}

public enum LedgerAccountType
{
    PlatformCash = 1,
    CustomerLiability = 2,
    SellerPending = 3,
    SellerAvailable = 4
}

public enum LedgerTransactionStatus
{
    Posted = 1,
    Reversed = 2
}

public sealed class LedgerTransaction : BaseEntity
{
    public string IdempotencyKey { get; set; } = string.Empty;
    public Guid OrderId { get; set; }
    public string? PayoutId { get; set; }
    public string Currency { get; set; } = "VND";
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
    public LedgerTransactionStatus Status { get; set; } = LedgerTransactionStatus.Posted;

    public ICollection<LedgerEntry> Entries { get; set; } = new List<LedgerEntry>();

    public void SetCreatedBy(string createdBy)
    {
        CreatedBy = createdBy;
    }
}

public sealed class LedgerEntry : BaseEntity
{
    public Guid LedgerTransactionId { get; set; }
    public LedgerTransaction LedgerTransaction { get; set; } = null!;

    public LedgerEntryType EntryType { get; set; }
    public LedgerAccountType AccountType { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "VND";
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;

    public void SetCreatedBy(string createdBy)
    {
        CreatedBy = createdBy;
    }
}

public sealed class BalanceSnapshot : BaseEntity
{
    public Guid LedgerTransactionId { get; set; }
    public LedgerTransaction LedgerTransaction { get; set; } = null!;

    public LedgerAccountType AccountType { get; set; }
    public decimal Balance { get; set; }
    public DateTime SnapshotAtUtc { get; set; } = DateTime.UtcNow;
}
