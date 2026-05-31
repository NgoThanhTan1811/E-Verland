namespace Modules.Payment.Application.Contracts;

public interface IWebhookIdempotencyService
{
    Task<bool> IsProcessedAsync(string transactionId, CancellationToken ct = default);
    Task<bool> TryMarkAsProcessedAsync(string transactionId, string paymentCode, string status, CancellationToken ct = default);
}
