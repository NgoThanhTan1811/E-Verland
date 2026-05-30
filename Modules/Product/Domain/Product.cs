using System.Text.Json.Serialization;
using SharedKernel.Entities;

namespace Modules.Product.Domain
{
    public class Product : BaseEntity
    {
        public string Name { get; set; } = default!;
        public string Description { get; set; } = default!;
        public decimal VirtualPrice { get; set; } = default!;
        public decimal BasePrice { get; set; } = default!;
        public string Slug { get; set; } = default!;

        public List<string> ImageUrls { get; set; } = [];
        public List<string>? VideoUrls { get; set; } = [];
        [JsonIgnore]
        public List<SKU> SKUs { get; set; } = [];
        public List<Category> Categories { get; set; } = [];
        public ProductStatus Status { get; set; } = ProductStatus.Published;

        public Dictionary<string, string> Attributes { get; set; } = [];

        public Guid? BrandId { get; set; }
        public Brand? Brand { get; set; }

        // Seller/Shop relationship
        public Guid? ShopId { get; set; }

        // Analytics and engagement
        public int ViewCount { get; set; } = 0;
        public int SoldCount { get; set; } = 0;
        public decimal? Rating { get; set; }
        public int ReviewCount { get; set; } = 0;
    }

    public enum ProductStatus
    {
        Draft,
        Published,
        Inactive,
        OutOfStock,
    }
}