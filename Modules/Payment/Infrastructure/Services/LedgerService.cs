using System.Data;
using Microsoft.EntityFrameworkCore;
using Modules.Payment.Application.Contracts;
using Modules.Payment.Domain;
using Modules.Payment.Infrastructure.Persistence;

namespace Modules.Payment.Infrastructure.Services;

public class LedgerService(PaymentDbContext dbContext) : ILedgerService
{
    private readonly PaymentDbContext _dbContext = dbContext;

    public Task<bool> RecordIncomingPaymentAsync(
        Guid orderId,
        decimal amount,
        string currency,
        string idempotencyKey,
        string createdBy,
        CancellationToken ct = default)
    {
        return RecordTransactionAsync(
            orderId,
            payoutId: null,
            amount,
            currency,
            idempotencyKey,
            createdBy,
            debitAccount: LedgerAccountType.CustomerLiability,
            creditAccount: LedgerAccountType.PlatformCash,
            ct);
    }

    public Task<bool> RecordSellerPayoutAsync(
        Guid orderId,
        string payoutId,
        decimal amount,
        string currency,
        string idempotencyKey,
        string createdBy,
        CancellationToken ct = default)
    {
        return RecordTransactionAsync(
            orderId,
            payoutId,
            amount,
            currency,
            idempotencyKey,
            createdBy,
            debitAccount: LedgerAccountType.SellerPending,
            creditAccount: LedgerAccountType.SellerAvailable,
            ct);
    }

    public Task<bool> RecordIncomingPaymentReversalAsync(
        Guid orderId,
        decimal amount,
        string currency,
        string idempotencyKey,
        string createdBy,
        CancellationToken ct = default)
    {
        return RecordTransactionAsync(
            orderId,
            payoutId: null,
            amount,
            currency,
            idempotencyKey,
            createdBy,
            debitAccount: LedgerAccountType.PlatformCash,
            creditAccount: LedgerAccountType.CustomerLiability,
            ct,
            status: LedgerTransactionStatus.Reversed);
    }

    public async Task<IReadOnlyList<LedgerEntryReadModel>> QueryEntriesAsync(
        Guid? orderId,
        string? payoutId,
        LedgerAccountType? accountType,
        DateTime? fromUtc,
        DateTime? toUtc,
        CancellationToken ct = default)
    {
        var query = _dbContext.LedgerEntries
            .AsNoTracking()
            .Include(x => x.LedgerTransaction)
            .AsQueryable();

        if (orderId.HasValue)
        {
            query = query.Where(x => x.LedgerTransaction.OrderId == orderId.Value);
        }

        if (!string.IsNullOrWhiteSpace(payoutId))
        {
            query = query.Where(x => x.LedgerTransaction.PayoutId == payoutId);
        }

        if (accountType.HasValue)
        {
            query = query.Where(x => x.AccountType == accountType.Value);
        }

        if (fromUtc.HasValue)
        {
            query = query.Where(x => x.TimestampUtc >= fromUtc.Value);
        }

        if (toUtc.HasValue)
        {
            query = query.Where(x => x.TimestampUtc <= toUtc.Value);
        }

        return await query
            .OrderByDescending(x => x.TimestampUtc)
            .Select(x => new LedgerEntryReadModel(
                x.LedgerTransactionId,
                x.LedgerTransaction.OrderId,
                x.LedgerTransaction.PayoutId,
                x.EntryType,
                x.AccountType,
                x.Amount,
                x.Currency,
                x.TimestampUtc,
                x.CreatedBy ?? "system",
                x.LedgerTransaction.Status))
            .ToListAsync(ct);
    }

    private async Task<bool> RecordTransactionAsync(
        Guid orderId,
        string? payoutId,
        decimal amount,
        string currency,
        string idempotencyKey,
        string createdBy,
        LedgerAccountType debitAccount,
        LedgerAccountType creditAccount,
        CancellationToken ct,
        LedgerTransactionStatus status = LedgerTransactionStatus.Posted)
    {
        if (amount <= 0)
        {
            throw new InvalidOperationException("Ledger amount must be greater than zero.");
        }

        var strategy = _dbContext.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            var alreadyExists = await _dbContext.LedgerTransactions
                .AsNoTracking()
                .AnyAsync(x => x.IdempotencyKey == idempotencyKey, ct);

            if (alreadyExists)
            {
                return false;
            }

            var debitAmount = amount;
            var creditAmount = amount;

            if (debitAmount != creditAmount)
            {
                throw new InvalidOperationException("Debit and credit totals must be equal for a ledger transaction.");
            }

            await using var tx = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);

            var ledgerTransaction = new LedgerTransaction
            {
                IdempotencyKey = idempotencyKey,
                OrderId = orderId,
                PayoutId = payoutId,
                Currency = currency,
                TimestampUtc = DateTime.UtcNow,
                Status = status
            };
            ledgerTransaction.SetCreatedBy(createdBy);

            var debitEntry = new LedgerEntry
            {
                LedgerTransactionId = ledgerTransaction.Id,
                EntryType = LedgerEntryType.Debit,
                AccountType = debitAccount,
                Amount = debitAmount,
                Currency = currency,
                TimestampUtc = DateTime.UtcNow
            };
            debitEntry.SetCreatedBy(createdBy);

            var creditEntry = new LedgerEntry
            {
                LedgerTransactionId = ledgerTransaction.Id,
                EntryType = LedgerEntryType.Credit,
                AccountType = creditAccount,
                Amount = creditAmount,
                Currency = currency,
                TimestampUtc = DateTime.UtcNow
            };
            creditEntry.SetCreatedBy(createdBy);

            _dbContext.LedgerTransactions.Add(ledgerTransaction);
            _dbContext.LedgerEntries.AddRange(debitEntry, creditEntry);

            await UpsertSnapshotAsync(ledgerTransaction.Id, debitAccount, -debitAmount, ct);
            await UpsertSnapshotAsync(ledgerTransaction.Id, creditAccount, creditAmount, ct);

            await _dbContext.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            return true;
        });
    }

    private async Task UpsertSnapshotAsync(
        Guid ledgerTransactionId,
        LedgerAccountType accountType,
        decimal delta,
        CancellationToken ct)
    {
        var lastBalance = await _dbContext.BalanceSnapshots
            .AsNoTracking()
            .Where(x => x.AccountType == accountType)
            .OrderByDescending(x => x.SnapshotAtUtc)
            .Select(x => x.Balance)
            .FirstOrDefaultAsync(ct);

        _dbContext.BalanceSnapshots.Add(new BalanceSnapshot
        {
            LedgerTransactionId = ledgerTransactionId,
            AccountType = accountType,
            Balance = lastBalance + delta,
            SnapshotAtUtc = DateTime.UtcNow
        });
    }
}
