using Modules.Product.Domain;
using SharedKernel.Pagination;

namespace Modules.Product.Application.DTOs.Request
{
    public record ProductVariantDto
    {
        public string Key { get; set; } = default!; // "Color", "Size"
        public List<string> Values { get; set; } = []; // ["Red", "Blue"]
    }

    public record CreateProductRequestDto
    {
        public string Name { get; set; } = default!;
        public string Description { get; set; } = default!;
        public decimal BasePrice { get; set; }
        public decimal VirtualPrice { get; set; }
        public int Stock { get; set; }
        public List<string> ImageUrls { get; set; } = [];
        public Dictionary<string, string> Attributes { get; set; } = [];
        public Guid? BrandId { get; set; }
        public List<Guid>? CategoryIds { get; set; } = [];
        public ProductStatus Status { get; set; } = ProductStatus.Draft;

        // For SKU auto-generation
        public List<ProductVariantDto>? Variants { get; set; }
    }

    public record UpdateProductRequestDto
    {
        public string Name { get; set; } = default!;
        public string Description { get; set; } = default!;
        public decimal BasePrice { get; set; }
        public decimal VirtualPrice { get; set; }
        public string Slug { get; set; } = default!;
        public List<string> ImageUrls { get; set; } = new List<string>();
        public Dictionary<string, string> Attributes { get; set; } = new Dictionary<string, string>();
        public Guid? BrandId { get; set; }
        public List<Guid> CategoryIds { get; set; } = new List<Guid>();
        public ProductStatus Status { get; set; } = ProductStatus.Draft;
    }

    public record FilterProductAdminRequestDto : IPagingFilter
    {
        public string? Keyword { get; set; }
        public Guid? CategoryId { get; set; }
        public Guid? BrandId { get; set; }
        public Guid? ShopId { get; set; }
        public ProductStatus? Status { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public string? SortBy { get; set; }
        public int Page { get; set; }
        public int Limit { get; set; }
    }

    public record FilterProductCustomerRequestDto : IPagingFilter
    {
        public string? Keyword { get; set; }
        public Guid? CategoryId { get; set; }
        public Guid? BrandId { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public int Page { get; set; }
        public int Limit { get; set; }
    }
}