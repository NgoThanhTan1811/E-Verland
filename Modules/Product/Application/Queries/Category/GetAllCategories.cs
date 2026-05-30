using MediatR;
using Modules.Product.Application.Contracts;
using Modules.Product.Application.DTOs.Response;

namespace Modules.Product.Application.Queries;

public sealed record GetAllCategoriesQuery() : IRequest<List<CategoryDetailDto>>;

public sealed class GetAllCategoriesHandler(ICategoryRepository categoryRepository) : IRequestHandler<GetAllCategoriesQuery, List<CategoryDetailDto>>
{
    private readonly ICategoryRepository _categoryRepository = categoryRepository;

    public async Task<List<CategoryDetailDto>> Handle(GetAllCategoriesQuery request, CancellationToken cancellationToken)
    {
        // Lấy tất cả danh mục từ Database nhưng chỉ lấy các trường cần thiết thông qua DTO
        var allCategories = await _categoryRepository.GetAllWithProductsAsync(cancellationToken);

        // Tách riêng danh mục cha và map danh mục con từ danh sách đã lấy
        var rootCategories = allCategories
            .Where(c => c.ParentCategoryId == null)
            .Select(category => new CategoryDetailDto
            {
                Id = category.Id,
                Name = category.Name,
                ParentCategoryId = category.ParentCategoryId,
                CreatedAt = category.CreatedAt,
                SubCategories = allCategories
                    .Where(c => c.ParentCategoryId == category.Id)
                    .Select(c => new CategoryListItemDto
                    {
                        Id = c.Id,
                        Name = c.Name,
                        ParentCategoryId = c.ParentCategoryId,
                        CreatedAt = c.CreatedAt
                    }).ToList()
            }).ToList();

        return rootCategories;

    }
}
