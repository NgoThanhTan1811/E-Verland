using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Modules.Product.Domain;

namespace Modules.Product.Application.DTOs.Response
{
    public record ProductAdminListItemDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = default!;
        public string Slug { get; set; } = default!;

        public decimal BasePrice { get; set; }
        public decimal VirtualPrice { get; set; }

        public string? BrandName { get; set; }
        public Guid? BrandId { get; set; }

        public List<string> CategoryNames { get; set; } = [];
        public Guid CategoryId { get; set; }

        public Dictionary<string, string> Attributes { get; set; } = [];

        public List<SkuAdminListItemDto> SKUs { get; set; } = [];

        public ProductStatus Status { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public record ProductListItemDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = default!;
        public string Description { get; set; } = default!;
        public decimal Price { get; set; }
        public List<string> ImageUrls { get; set; } = [];

        public Guid? ShopId { get; set; }
        public string? ShopName { get; set; }

        public string? BrandName { get; set; }
        public Guid? BrandId { get; set; }

        public List<string> CategoryNames { get; set; } = [];
        public Guid CategoryId { get; set; }

        public Dictionary<string, string> Attributes { get; set; } = [];

        public List<SkuDetailDto> SKUs { get; set; } = [];

        public ProductStatus Status { get; set; }

    }

    public record ProductDetailDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = default!;
        public string Description { get; set; } = default!;

        public decimal Price { get; set; }

        public List<string> ImageUrls { get; set; } = [];
        public Dictionary<string, string> Attributes { get; set; } = [];

        public Guid? ShopId { get; set; }
        public string? ShopName { get; set; }
        public ProductBrandDto? Brand { get; set; }
        public List<ProductCategoryDto> Categories { get; set; } = [];
        public List<SkuDetailDto> Skus { get; set; } = [];
    }

    public record ProductBrandDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = default!;
    }

    public record ProductCategoryDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = default!;
    }
}