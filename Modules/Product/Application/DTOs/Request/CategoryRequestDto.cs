using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Modules.Product.Application.DTOs.Request
{
    public record CreateCategoryRequestDto
    {
        public string Name { get; set; } = default!;
        public Guid? ParentCategoryId { get; set; }
    }

    public record UpdateCategoryRequestDto
    {
        public string Name { get; set; } = default!;
        public Guid? ParentCategoryId { get; set; }
    }
}