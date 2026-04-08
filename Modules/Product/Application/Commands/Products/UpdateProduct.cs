using Amazon.XRay.Recorder.Core;
using Infra.AWS.CloudWatch;
using MediatR;
using Modules.Product.Application.Contracts;
using Modules.Product.Application.DTOs.Request;
using Modules.Product.Application.DTOs.Response;

namespace Modules.Product.Application.Commands;

public sealed record UpdateProductCommand(Guid Id, UpdateProductRequestDto Request) : IRequest<ProductDetailDto>;

public sealed class UpdateProductHandler(
    IProductRepository productRepository,
    ICategoryRepository categoryRepository,
    IProductDbContext dbContext,
    IProductSyncPublisher syncPublisher,
    ICloudWatchService cloudWatch) : IRequestHandler<UpdateProductCommand, ProductDetailDto>
{
    private readonly IProductRepository _productRepository = productRepository;
    private readonly ICategoryRepository _categoryRepository = categoryRepository;
    private readonly IProductDbContext _dbContext = dbContext;
    private readonly IProductSyncPublisher _syncPublisher = syncPublisher;
    private readonly ICloudWatchService _cloudWatch = cloudWatch;

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

        AWSXRayRecorder.Instance.BeginSubsegment("Product.DB");
        try
        {
            await _productRepository.UpdateAsync(product, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            AWSXRayRecorder.Instance.AddException(ex);
            throw;
        }
        finally
        {
            AWSXRayRecorder.Instance.EndSubsegment();
        }

        await _syncPublisher.PublishAsync(product, "Updated", cancellationToken);
        await _cloudWatch.PutMetricAsync("product.updated", 1, "Count", ct: cancellationToken);

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
