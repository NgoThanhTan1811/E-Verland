namespace Modules.Cart.Application.DTOs.Request;

public record AddToCartRequestDto
{
    public required Guid ProductId { get; set; }
    public Guid? SkuId { get; set; }
    public required int Quantity { get; set; }
}
