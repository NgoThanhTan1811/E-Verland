using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SharedKernel.Entities;

namespace Modules.Product.Domain
{
    public class Category : BaseEntity
    {
        public string Name { get; set; } = default!;
        public List<Product>? Products { get; set; } = [];
        public Guid? ParentCategoryId { get; set; }
        public Category? ParentCategory { get; set; }
        public List<Category>? SubCategories { get; set; } = [];
         
    }
}