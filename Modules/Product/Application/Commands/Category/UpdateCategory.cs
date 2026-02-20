using MediatR;
using Modules.Product.Application.Abtracsts;
using Modules.Product.Application.DTOs.Request;
using Modules.Product.Application.DTOs.Response;

namespace Modules.Product.Application.Commands;

public sealed record UpdateCategoryCommand(Guid Id, UpdateCategoryRequestDto Request) : IRequest<CategoryDetailDto>;

public sealed class UpdateCategoryHandler : IRequestHandler<UpdateCategoryCommand, CategoryDetailDto>
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IProductDbContext _dbContext;

    public UpdateCategoryHandler(ICategoryRepository categoryRepository, IProductDbContext dbContext)
    {
        _categoryRepository = categoryRepository;
        _dbContext = dbContext;
    }

    public async Task<CategoryDetailDto> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await _categoryRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Category with ID '{request.Id}' not found.");

        if (category.Name != request.Request.Name)
        {
            var existingCategory = await _categoryRepository.GetByNameAsync(request.Request.Name, cancellationToken);
            if (existingCategory != null)
                throw new InvalidOperationException($"Category with name '{request.Request.Name}' already exists.");
        }

        if (category.ParentCategoryId != request.Request.ParentCategoryId && request.Request.ParentCategoryId.HasValue)
        {
            var parentCategory = await _categoryRepository.GetByIdAsync(request.Request.ParentCategoryId.Value, cancellationToken);
            if (parentCategory == null)
                throw new KeyNotFoundException($"Parent category with ID '{request.Request.ParentCategoryId}' not found.");
        }

        category.Name = request.Request.Name;
        category.ParentCategoryId = request.Request.ParentCategoryId;

        await _categoryRepository.UpdateAsync(category, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var subCategories = await _categoryRepository.GetSubCategoriesAsync(request.Id, cancellationToken);
        var subCategoryDtos = subCategories.Select(c => new CategoryListItemDto
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
