namespace Modules.Cart.Application.DTOs.Response;

public class CartItemResponseDto
{
    public Guid Id { get; set; }
    public Guid CartId { get; set; }
    public Guid ProductId { get; set; }
    public Guid SkuId { get; set; }
    public int Quantity { get; set; }
    public string ProductName { get; set; } = default!;
    public string? ProductImage { get; set; }
    public string SkuValue { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
