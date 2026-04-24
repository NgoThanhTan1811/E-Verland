using Infra.AWS.CloudWatch;
using MediatR;
using Modules.Product.Application.Contracts;
using Modules.Product.Domain;

namespace Modules.Product.Application.Commands;

public sealed record HideProductByAdminCommand(Guid ProductId, Guid AdminId, string Reason) : IRequest<bool>;

public sealed class HideProductByAdminHandler(
    IProductRepository productRepository,
    IProductDbContext dbContext,
    IProductSyncPublisher syncPublisher,
    IProductModerationAuditLog moderationAuditLog,
    ICloudWatchService cloudWatch) : IRequestHandler<HideProductByAdminCommand, bool>
{
    private readonly IProductRepository _productRepository = productRepository;
    private readonly IProductDbContext _dbContext = dbContext;
    private readonly IProductSyncPublisher _syncPublisher = syncPublisher;
    private readonly IProductModerationAuditLog _moderationAuditLog = moderationAuditLog;
    private readonly ICloudWatchService _cloudWatch = cloudWatch;

    public async Task<bool> Handle(HideProductByAdminCommand request, CancellationToken cancellationToken)
    {
        var product = await _productRepository.ChangeStatusAsync(request.ProductId, ProductStatus.Inactive, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _moderationAuditLog.WriteAsync(request.AdminId, "Hide", product.Id, request.Reason, DateTime.UtcNow, cancellationToken);
        await _syncPublisher.PublishModerationAsync(product, "Hide", request.AdminId, request.Reason, cancellationToken);
        await _cloudWatch.PutMetricAsync("product.moderated.hidden", 1, "Count", ct: cancellationToken);
        return true;
    }
}

public sealed record SoftDeleteProductByAdminCommand(Guid ProductId, Guid AdminId, string Reason) : IRequest<bool>;

public sealed class SoftDeleteProductByAdminHandler(
    IProductRepository productRepository,
    IProductDbContext dbContext,
    IProductSyncPublisher syncPublisher,
    IProductModerationAuditLog moderationAuditLog,
    ICloudWatchService cloudWatch) : IRequestHandler<SoftDeleteProductByAdminCommand, bool>
{
    private readonly IProductRepository _productRepository = productRepository;
    private readonly IProductDbContext _dbContext = dbContext;
    private readonly IProductSyncPublisher _syncPublisher = syncPublisher;
    private readonly IProductModerationAuditLog _moderationAuditLog = moderationAuditLog;
    private readonly ICloudWatchService _cloudWatch = cloudWatch;

    public async Task<bool> Handle(SoftDeleteProductByAdminCommand request, CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByIdAsync(request.ProductId, cancellationToken)
            ?? throw new KeyNotFoundException($"Product with ID '{request.ProductId}' not found.");

        var deleted = await _productRepository.DeleteAsync(request.ProductId, cancellationToken);
        if (!deleted)
        {
            throw new KeyNotFoundException($"Product with ID '{request.ProductId}' not found.");
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _moderationAuditLog.WriteAsync(request.AdminId, "Delete", product.Id, request.Reason, DateTime.UtcNow, cancellationToken);
        await _syncPublisher.PublishModerationAsync(product, "Delete", request.AdminId, request.Reason, cancellationToken);
        await _cloudWatch.PutMetricAsync("product.moderated.deleted", 1, "Count", ct: cancellationToken);
        return true;
    }
}

public sealed record RestoreProductByAdminCommand(Guid ProductId, Guid AdminId, string Reason) : IRequest<bool>;

public sealed class RestoreProductByAdminHandler(
    IProductRepository productRepository,
    IProductDbContext dbContext,
    IProductModerationAuditLog moderationAuditLog,
    ICloudWatchService cloudWatch) : IRequestHandler<RestoreProductByAdminCommand, bool>
{
    private readonly IProductRepository _productRepository = productRepository;
    private readonly IProductDbContext _dbContext = dbContext;
    private readonly IProductModerationAuditLog _moderationAuditLog = moderationAuditLog;
    private readonly ICloudWatchService _cloudWatch = cloudWatch;

    public async Task<bool> Handle(RestoreProductByAdminCommand request, CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByIdIncludingDeletedAsync(request.ProductId, cancellationToken)
            ?? throw new KeyNotFoundException($"Product with ID '{request.ProductId}' not found.");

        var restored = await _productRepository.RestoreAsync(request.ProductId, cancellationToken);
        if (!restored)
        {
            throw new KeyNotFoundException($"Product with ID '{request.ProductId}' not found.");
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _moderationAuditLog.WriteAsync(request.AdminId, "Restore", product.Id, request.Reason, DateTime.UtcNow, cancellationToken);
        await _cloudWatch.PutMetricAsync("product.moderated.restored", 1, "Count", ct: cancellationToken);
        return true;
    }
}
