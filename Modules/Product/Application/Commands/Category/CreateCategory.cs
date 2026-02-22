using MediatR;
using Modules.Product.Application.Contracts;
using Modules.Product.Application.DTOs.Request;
using Modules.Product.Application.DTOs.Response;

namespace Modules.Product.Application.Commands;

public sealed record CreateCategoryCommand(CreateCategoryRequestDto Request) : IRequest<CategoryDetailDto>;

public sealed class CreateCategoryHandler : IRequestHandler<CreateCategoryCommand, CategoryDetailDto>
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IProductDbContext _dbContext;

    public CreateCategoryHandler(ICategoryRepository categoryRepository, IProductDbContext dbContext)
    {
        _categoryRepository = categoryRepository;
        _dbContext = dbContext;
    }

    public async Task<CategoryDetailDto> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        var existingCategory = await _categoryRepository.GetByNameAsync(request.Request.Name, cancellationToken);
        if (existingCategory != null)
            throw new InvalidOperationException($"Category with name '{request.Request.Name}' already exists.");

        if (request.Request.ParentCategoryId.HasValue)
        {
            var parentCategory = await _categoryRepository.GetByIdAsync(request.Request.ParentCategoryId.Value, cancellationToken);
            if (parentCategory == null)
                throw new KeyNotFoundException($"Parent category with ID '{request.Request.ParentCategoryId}' not found.");
        }

        var category = new Domain.Category
        {
            Name = request.Request.Name,
            ParentCategoryId = request.Request.ParentCategoryId
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
}
