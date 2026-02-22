using MediatR;
using Modules.Product.Application.Contracts;
using Modules.Product.Application.DTOs.Response;

namespace Modules.Product.Application.Queries;

public sealed record GetCategoryByIdQuery(Guid Id) : IRequest<CategoryDetailDto?>;

public sealed class GetCategoryByIdHandler : IRequestHandler<GetCategoryByIdQuery, CategoryDetailDto?>
{
    private readonly ICategoryRepository _categoryRepository;

    public GetCategoryByIdHandler(ICategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    public async Task<CategoryDetailDto?> Handle(GetCategoryByIdQuery request, CancellationToken cancellationToken)
    {
        var category = await _categoryRepository.GetByIdWithSubCategoriesAsync(request.Id, cancellationToken);
        if (category == null)
            return null;

        var subCategoryDtos = category.SubCategories.Select(c => new CategoryListItemDto
        {
            Id = c.Id,
            Name = c.Name,
            ParentCategoryId = c.ParentCategoryId,
            CreatedAt = c.CreatedAt
        }).ToList();

        return new CategoryDetailDto
        {
            Id = category.Id,
            Name = category.Name,
            ParentCategoryId = category.ParentCategoryId,
            SubCategories = subCategoryDtos,
            CreatedAt = category.CreatedAt,
            UpdatedAt = category.UpdatedAt
        };
    }
}
