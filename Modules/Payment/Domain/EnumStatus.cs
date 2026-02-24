namespace Modules.Payment.Domain
{
    public enum PaymentStatus
    {
        Pending,
        Success,
        Failed,
        Refunded
    }

    public enum PaymentMethod
    {
        OnlineBanking,
        COD
    }
}