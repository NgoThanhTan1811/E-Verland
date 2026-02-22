namespace Modules.Order.Domain
{
    public enum OrderStatus
    {
        Pending,
        Confirmed,
        Shipping,
        Canceled,
        Completed
    }

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