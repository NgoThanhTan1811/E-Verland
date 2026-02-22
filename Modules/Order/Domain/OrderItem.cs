using Modules.Order.Domain;
using SharedKernel.Entities;

public sealed class OrderItem : BaseEntity
{
    public Guid OrderId { get; private set; }
    public Guid ProductId { get; private set; }
    public Guid SkuId { get; private set; }

    public string ProductName { get; private set; } = default!;
    public int UnitPrice { get; private set; }
    public int Quantity { get; private set; }

    public int TotalPrice => UnitPrice * Quantity;

    public Order Order { get; private set; } = default!;

    private OrderItem() { }

    public OrderItem(Guid productId, Guid skuId, string productName, int unitPrice, int quantity)
    {
        if (string.IsNullOrWhiteSpace(productName))
            throw new ArgumentException("ProductName is required");

        if (unitPrice < 0)
            throw new ArgumentException("UnitPrice must be >= 0");

        if (quantity <= 0)
            throw new ArgumentException("Quantity must be > 0");

        ProductId = productId;
        SkuId = skuId;
        ProductName = productName;
        UnitPrice = unitPrice;
        Quantity = quantity;
    }
}