using MediatR;
using Modules.Product.Application.Contracts;
using Modules.Product.Application.DTOs.Request;
using Modules.Product.Application.DTOs.Response;

namespace Modules.Product.Application.Commands;

public sealed record UpdateProductCommand(Guid Id, UpdateProductRequestDto Request) : IRequest<ProductDetailDto>;

public sealed class UpdateProductHandler : IRequestHandler<UpdateProductCommand, ProductDetailDto>
{
    private readonly IProductRepository _productRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IProductDbContext _dbContext;

    public UpdateProductHandler(IProductRepository productRepository, ICategoryRepository categoryRepository, IProductDbContext dbContext)
    {
        _productRepository = productRepository;
        _categoryRepository = categoryRepository;
        _dbContext = dbContext;
    }

    public async Task<ProductDetailDto> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Product with ID '{request.Id}' not found.");

        // Validate categories exist
        var categories = new List<Domain.Category>();
        if (request.Request.CategoryIds.Any())
        {
            foreach (var categoryId in request.Request.CategoryIds)
            {
                var category = await _categoryRepository.GetByIdAsync(categoryId, cancellationToken);
                if (category == null)
                    throw new KeyNotFoundException($"Category with ID '{categoryId}' not found.");
                categories.Add(category);
            }
        }

        product.Name = request.Request.Name;
        product.Description = request.Request.Description;
        product.BasePrice = request.Request.BasePrice;
        product.VirtualPrice = request.Request.VirtualPrice;
        product.Slug = request.Request.Slug;
        product.ImageUrls = request.Request.ImageUrls;
        product.Attributes = request.Request.Attributes;
        product.BrandId = request.Request.BrandId;
        product.Status = request.Request.Status;
        product.Categories = categories;

        await _productRepository.UpdateAsync(product, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapToDetailDto(product);
    }

    private static ProductDetailDto MapToDetailDto(Domain.Product product)
    {
        return new ProductDetailDto
        {
            Id = product.Id,
            Name = product.Name,
            Slug = product.Slug,
            Description = product.Description,
            BasePrice = product.BasePrice,
            VirtualPrice = product.VirtualPrice,
            ImageUrls = product.ImageUrls,
            Attributes = product.Attributes,
            Brand = product.Brand,
            Categories = product.Categories,
            Skus = product.SKUs
        };
    }
}
