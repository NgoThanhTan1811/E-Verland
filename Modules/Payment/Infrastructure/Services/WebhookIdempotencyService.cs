using Modules.Payment.Domain;

namespace Modules.Payment.Infrastructure.Services;

/// <summary>
/// Tracks webhook processing to ensure idempotency
/// </summary>
public interface IWebhookIdempotencyService
{
    Task<bool> IsProcessedAsync(string webhookId, CancellationToken ct = default);
    Task MarkAsProcessedAsync(string webhookId, string paymentCode, string status, CancellationToken ct = default);
}

public class WebhookIdempotencyService : IWebhookIdempotencyService
{
    private readonly Dictionary<string, WebhookRecord> _processedWebhooks = new();
    private readonly SemaphoreSlim _lock = new(1, 1);

    public async Task<bool> IsProcessedAsync(string webhookId, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            return _processedWebhooks.ContainsKey(webhookId);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task MarkAsProcessedAsync(string webhookId, string paymentCode, string status, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            _processedWebhooks[webhookId] = new WebhookRecord(
                webhookId,
                paymentCode,
                status,
                DateTime.UtcNow
            );

            // Cleanup old entries (keep last 1000)
            if (_processedWebhooks.Count > 1000)
            {
                var oldest = _processedWebhooks
                    .OrderBy(kv => kv.Value.ProcessedAt)
                    .Take(100)
                    .Select(kv => kv.Key)
                    .ToList();

                foreach (var key in oldest)
                {
                    _processedWebhooks.Remove(key);
                }
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    private record WebhookRecord(string WebhookId, string PaymentCode, string Status, DateTime ProcessedAt);
}
