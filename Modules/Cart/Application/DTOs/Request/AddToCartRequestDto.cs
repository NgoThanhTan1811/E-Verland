namespace Modules.Cart.Application.DTOs.Request;

public record AddToCartRequestDto
{
    public required Guid ProductId { get; set; }
    public required Guid SkuId { get; set; }
    public required int Quantity { get; set; }
    public required string ProductName { get; set; }
    public string? ProductImage { get; set; }
    public required string SkuValue { get; set; }
}
