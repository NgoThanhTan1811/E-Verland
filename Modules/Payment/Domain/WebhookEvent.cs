using SharedKernel.Entities;

namespace Modules.Payment.Domain;

public sealed class WebhookEvent : BaseEntity
{
    public string TransactionId { get; set; } = string.Empty;
    public string PaymentCode { get; set; } = string.Empty;
    public string EventStatus { get; set; } = string.Empty;
    public DateTime ProcessedAtUtc { get; set; } = DateTime.UtcNow;
}
