namespace Modules.Cart.Application.DTOs.Response;

public class CartResponseDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public int TotalItems { get; set; }
    public List<CartItemResponseDto> Items { get; set; } = [];
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
