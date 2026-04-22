using Amazon.XRay.Recorder.Core;
using Infra.AWS.CloudWatch;
using MediatR;
using Modules.Media.Application.Interfaces;
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
    ICloudWatchService cloudWatch,
    IMediaFileRepository mediaFileRepository) : IRequestHandler<UpdateProductCommand, ProductDetailDto>
{
    private readonly IProductRepository _productRepository = productRepository;
    private readonly ICategoryRepository _categoryRepository = categoryRepository;
    private readonly IProductDbContext _dbContext = dbContext;
    private readonly IProductSyncPublisher _syncPublisher = syncPublisher;
    private readonly ICloudWatchService _cloudWatch = cloudWatch;
    private readonly IMediaFileRepository _mediaFileRepository = mediaFileRepository;

    public async Task<ProductDetailDto> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        await ValidateImagePathsAsync(request.Request.ImageUrls, cancellationToken);

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

        foreach (var imagePath in request.Request.ImageUrls)
        {
            await _mediaFileRepository.ConfirmByPathAsync(imagePath, cancellationToken);
        }

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

    private async Task ValidateImagePathsAsync(IEnumerable<string> imagePaths, CancellationToken ct)
    {
        foreach (var imagePath in imagePaths)
        {
            if (!IsValidRelativePath(imagePath))
                throw new InvalidOperationException($"Invalid image relative path: '{imagePath}'. External URLs are not allowed.");

            var media = await _mediaFileRepository.GetByPathAsync(imagePath, ct);
            if (media == null)
                throw new InvalidOperationException($"Media path does not exist: '{imagePath}'.");
        }
    }

    private static bool IsValidRelativePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        if (Uri.TryCreate(path, UriKind.Absolute, out _))
            return false;

        return path.StartsWith("products/", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("avatars/", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("shops/", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("reviews/", StringComparison.OrdinalIgnoreCase);
    }
}
