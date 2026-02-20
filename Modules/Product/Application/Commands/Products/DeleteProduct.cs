using MediatR;
using Modules.Product.Application.Abtracsts;

namespace Modules.Product.Application.Commands;

public sealed record DeleteProductCommand(Guid Id) : IRequest<bool>;

public sealed class DeleteProductHandler : IRequestHandler<DeleteProductCommand, bool>
{
    private readonly IProductRepository _productRepository;
    private readonly IProductDbContext _dbContext;

    public DeleteProductHandler(IProductRepository productRepository, IProductDbContext dbContext)
    {
        _productRepository = productRepository;
        _dbContext = dbContext;
    }

    public async Task<bool> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        var result = await _productRepository.DeleteAsync(request.Id, cancellationToken);
        if (!result)
            throw new KeyNotFoundException($"Product with ID '{request.Id}' not found.");

        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
