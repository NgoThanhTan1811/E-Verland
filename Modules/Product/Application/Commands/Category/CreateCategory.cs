using MediatR;
using Modules.Product.Application.Contracts;
using Modules.Product.Application.DTOs.Request;
using Modules.Product.Application.DTOs.Response;

namespace Modules.Product.Application.Commands;

public sealed record CreateCategoryCommand(CreateCategoryRequestDto Request) : IRequest<CategoryDetailDto>;

public sealed class CreateCategoryHandler(ICategoryRepository categoryRepository, IProductDbContext dbContext) : IRequestHandler<CreateCategoryCommand, CategoryDetailDto>
{
    private readonly ICategoryRepository _categoryRepository = categoryRepository;
    private readonly IProductDbContext _dbContext = dbContext;

    public async Task<CategoryDetailDto> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        var parentCategoryId = NormalizeParentCategoryId(request.Request.ParentCategoryId);

        var existingCategory = await _categoryRepository.GetByNameAsync(request.Request.Name, cancellationToken);
        if (existingCategory != null)
            throw new InvalidOperationException($"Category with name '{request.Request.Name}' already exists.");

        if (parentCategoryId.HasValue)
        {
            var parentCategory = await _categoryRepository.GetByIdAsync(parentCategoryId.Value, cancellationToken);
            if (parentCategory == null)
                throw new KeyNotFoundException($"Parent category with ID '{parentCategoryId}' not found.");
        }

        var category = new Domain.Category
        {
            Name = request.Request.Name,
            ParentCategoryId = parentCategoryId
        };

        await _categoryRepository.CreateAsync(category, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new CategoryDetailDto
        {
            Id = category.Id,
            Name = category.Name,
            ParentCategoryId = category.ParentCategoryId,
            SubCategories = new List<CategoryListItemDto>(),
            CreatedAt = category.CreatedAt,
            UpdatedAt = category.UpdatedAt
        };
    }

    private static Guid? NormalizeParentCategoryId(Guid? parentCategoryId)
    {
        if (!parentCategoryId.HasValue || parentCategoryId.Value == Guid.Empty)
            return null;

        return parentCategoryId;
    }
}
