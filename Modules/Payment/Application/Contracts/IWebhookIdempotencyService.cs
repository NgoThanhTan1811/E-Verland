namespace Modules.Payment.Application.Contracts;

public interface IWebhookIdempotencyService
{
    Task<bool> IsProcessedAsync(string idempotencyKey, CancellationToken ct = default);
    Task<bool> TryMarkAsProcessedAsync(string idempotencyKey, string paymentCode, string status, CancellationToken ct = default);
}
