namespace Modules.Shipping.Domain;

public enum ShippingStatus
{
    Draft,
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
