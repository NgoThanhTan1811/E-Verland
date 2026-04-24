namespace Modules.Payment.Infrastructure.Services;

public class SePayApiException : Exception
{
    public string? TransactionId { get; }
    public int? StatusCode { get; }

    public SePayApiException() : base("SePay API error occurred")
    {
    }

    public SePayApiException(string message) : base(message)
    {
    }

    public SePayApiException(string message, Exception innerException) : base(message, innerException)
    {
    }

    public SePayApiException(string message, string? transactionId, int? statusCode = null)
        : base(message)
    {
        TransactionId = transactionId;
        StatusCode = statusCode;
    }
}

public class SePayWebhookException : Exception
{
    public string? PaymentCode { get; }

    public SePayWebhookException() : base("SePay webhook error occurred")
    {
    }

    public SePayWebhookException(string message) : base(message)
    {
    }

    public SePayWebhookException(string message, Exception innerException) : base(message, innerException)
    {
    }

    public SePayWebhookException(string message, string? paymentCode) : base(message)
    {
        PaymentCode = paymentCode;
    }
}