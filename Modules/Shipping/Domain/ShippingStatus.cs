namespace Modules.Shipping.Domain;

public enum ShippingStatus
{
    Pending,
    Created,
    Picking,
    Picked,
    Delivering,
    Delivered,
    Returned,
    Canceled,
    Failed
}
