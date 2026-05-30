
using SharedKernel.Entities;

namespace Modules.Cart.Domain
{
    public class CartItem : BaseEntity
    {
        public Guid CartId { get; set; }
        public Guid ProductId { get; set; }
        public int Quantity { get; set; }
        public Guid? SkuId { get; set; }

        public string ProductName { get; set; } = null!;
        public string? ProductImage { get; set; }
        public string? SkuValue { get; set; }


        public Cart Cart { get; set; } = null!;
    }
}