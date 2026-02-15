using SharedKernel.Entities;

namespace Modules.Product.Domain
{
    public class Product : BaseEntity
    {
        public string Name { get; set; } = default!;
        public string Description { get; set; } = default!;
        public decimal VirtualPrice { get; set; } = default!;
        public decimal BasePrice { get; set; } = default!;

        public List<string>? ImageUrls { get; set; }
        public List<SKU> SKUs { get; set; } = [];
        public List<Category> Categories { get; set; } = [];
        public ProductStatus Status { get; set; } = ProductStatus.Pending;

        public Dictionary<string, string>? Attributes { get; set; } 
        public Guid? BrandId { get; set; }
        public Brand? Brand { get; set; } 

    }

    public enum ProductStatus
    {
        Active,
        Inactive,
        OutOfStock,
        Pending,
    }
}