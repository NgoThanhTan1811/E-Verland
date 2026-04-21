using Microsoft.EntityFrameworkCore;
using Modules.Payment.Application.Contracts;
using Modules.Payment.Domain;
using Modules.Payment.Infrastructure.Persistence;

namespace Modules.Payment.Infrastructure.Services;

public class SellerBalanceService(PaymentDbContext dbContext, ILedgerService ledgerService) : ISellerBalanceService
{
    private readonly PaymentDbContext _dbContext = dbContext;
    private readonly ILedgerService _ledgerService = ledgerService;

    public async Task EnsurePendingBalanceAsync(
        Guid orderId,
        Guid sellerId,
        decimal amount,
        string currency,
        DateTime availableAtUtc,
        CancellationToken ct = default)
    {
        var existing = await _dbContext.SellerBalances
            .FirstOrDefaultAsync(x => x.OrderId == orderId, ct);

        if (existing is not null)
        {
            return;
        }

        _dbContext.SellerBalances.Add(new SellerBalance
        {
            OrderId = orderId,
            SellerId = sellerId,
            PendingAmount = amount,
            AvailableAmount = 0,
            Currency = currency,
            AvailableAtUtc = availableAtUtc,
            Status = SellerBalanceStatus.Pending
        });

        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task<int> ProcessDuePayoutsAsync(CancellationToken ct = default)
    {
        var nowUtc = DateTime.UtcNow;

        var dueBalances = await _dbContext.SellerBalances
            .Where(x => x.Status == SellerBalanceStatus.Pending && x.AvailableAtUtc <= nowUtc)
            .ToListAsync(ct);

        var released = 0;

        foreach (var balance in dueBalances)
        {
            var payoutId = balance.PayoutId ?? $"payout-{balance.OrderId:N}-{nowUtc:yyyyMMdd}";
            var key = $"seller-payout:{balance.OrderId:N}:{payoutId}";

            var posted = await _ledgerService.RecordSellerPayoutAsync(
                balance.OrderId,
                payoutId,
                balance.PendingAmount,
                balance.Currency,
                key,
                "background-job",
                ct);

            if (!posted)
            {
                continue;
            }

            balance.PayoutId = payoutId;
            balance.AvailableAmount += balance.PendingAmount;
            balance.PendingAmount = 0;
            balance.Status = SellerBalanceStatus.Available;
            released++;
        }

        if (released > 0)
        {
            await _dbContext.SaveChangesAsync(ct);
        }

        return released;
    }
}
