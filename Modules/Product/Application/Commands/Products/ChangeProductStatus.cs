using MediatR;
using Modules.Product.Application.Contracts;
using Modules.Product.Domain;

namespace Modules.Product.Application.Commands;

public sealed record ChangeProductStatusCommand(Guid ProductId, ProductStatus Status) : IRequest<bool>;

public sealed class ChangeProductStatusHandler(IProductRepository productRepository, IProductDbContext dbContext) : IRequestHandler<ChangeProductStatusCommand, bool>
{
    private readonly IProductRepository _productRepository = productRepository;
    private readonly IProductDbContext _dbContext = dbContext;

    public async Task<bool> Handle(ChangeProductStatusCommand request, CancellationToken cancellationToken)
    {
        await _productRepository.ChangeStatusAsync(request.ProductId, request.Status, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}
