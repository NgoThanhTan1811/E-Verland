using System;
using System.Collections.Generic;

namespace Modules.Product.Application.DTOs.Response
{
    public record CategoryListItemDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = default!;
        public Guid? ParentCategoryId { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public record CategoryDetailDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = default!;
        public Guid? ParentCategoryId { get; set; }
        public List<CategoryListItemDto> SubCategories { get; set; } = new List<CategoryListItemDto>();
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
