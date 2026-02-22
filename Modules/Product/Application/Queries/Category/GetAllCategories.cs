using MediatR;
using Modules.Product.Application.Contracts;
using Modules.Product.Application.DTOs.Response;

namespace Modules.Product.Application.Queries;

public sealed record GetAllCategoriesQuery() : IRequest<List<CategoryDetailDto>>;

public sealed class GetAllCategoriesHandler : IRequestHandler<GetAllCategoriesQuery, List<CategoryDetailDto>>
{
    private readonly ICategoryRepository _categoryRepository;

    public GetAllCategoriesHandler(ICategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    public async Task<List<CategoryDetailDto>> Handle(GetAllCategoriesQuery request, CancellationToken cancellationToken)
    {
        var categories = await _categoryRepository.GetAllWithProductsAsync(cancellationToken);

        var dtos = new List<CategoryDetailDto>();
        foreach (var category in categories.Where(c => c.ParentCategoryId == null))
        {
            var subCategoryDtos = categories
                .Where(c => c.ParentCategoryId == category.Id)
                .Select(c => new CategoryListItemDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    ParentCategoryId = c.ParentCategoryId,
                    CreatedAt = c.CreatedAt
                }).ToList();

            dtos.Add(new CategoryDetailDto
            {
                Id = category.Id,
                Name = category.Name,
                ParentCategoryId = category.ParentCategoryId,
                SubCategories = subCategoryDtos,
                CreatedAt = category.CreatedAt,
                UpdatedAt = category.UpdatedAt
            });
        }

        return dtos;
    }
}
