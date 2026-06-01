using Microsoft.EntityFrameworkCore;
using Modules.Payment.Application.Contracts;
using Modules.Payment.Domain;
using Modules.Payment.Infrastructure.Persistence;

namespace Modules.Payment.Infrastructure.Services;

public class WebhookIdempotencyService(PaymentDbContext dbContext) : IWebhookIdempotencyService
{
    private readonly PaymentDbContext _dbContext = dbContext;

    public Task<bool> IsProcessedAsync(string transactionId, CancellationToken ct = default)
    {
        return _dbContext.WebhookEvents
            .AsNoTracking()
            .AnyAsync(x => x.TransactionId == transactionId, ct);
    }

    public async Task<bool> TryMarkAsProcessedAsync(string transactionId, string paymentCode, string status, CancellationToken ct = default)
    {
        var entry = new WebhookEvent
        {
            TransactionId = transactionId,
            PaymentCode = paymentCode,
            EventStatus = status,
            ProcessedAtUtc = DateTime.UtcNow
        };

        _dbContext.WebhookEvents.Add(entry);

        try
        {
            await _dbContext.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateException)
        {
            return false;
        }
    }
}
