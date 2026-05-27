using Amazon.XRay.Recorder.Core;
using Infra.AWS.CloudWatch;
using MediatR;
using Modules.Product.Application.Contracts;

namespace Modules.Product.Application.Commands;

public sealed record DeleteProductCommand(Guid Id) : IRequest<bool>;

public sealed class DeleteProductHandler(
    IProductRepository productRepository,
    IProductDbContext dbContext,
    IProductSyncPublisher syncPublisher,
    ICloudWatchService cloudWatch) : IRequestHandler<DeleteProductCommand, bool>
{
    private readonly IProductRepository _productRepository = productRepository;
    private readonly IProductDbContext _dbContext = dbContext;
    private readonly IProductSyncPublisher _syncPublisher = syncPublisher;
    private readonly ICloudWatchService _cloudWatch = cloudWatch;

    public async Task<bool> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Product with ID '{request.Id}' not found.");

        AWSXRayRecorder.Instance.BeginSubsegment("Product.DB");
        try
        {
            var result = await _productRepository.DeleteAsync(request.Id, cancellationToken);
            if (!result)
                throw new KeyNotFoundException($"Product with ID '{request.Id}' not found.");

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

        await _syncPublisher.PublishAsync(product, "Deleted", cancellationToken);
        await _cloudWatch.PutMetricAsync("product.deleted", 1, "Count", ct: cancellationToken);

        return true;
    }
}
