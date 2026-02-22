namespace Modules.Cart.Application.DTOs.Request;

public record UpdateCartItemRequestDto
{
    public required Guid CartItemId { get; set; }
    public required int Quantity { get; set; }
}
