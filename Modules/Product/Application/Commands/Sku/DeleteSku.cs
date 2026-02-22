using MediatR;
using Modules.Product.Application.Contracts;

namespace Modules.Product.Application.Commands;

public sealed record DeleteSkuCommand(Guid Id) : IRequest<bool>;

public sealed class DeleteSkuHandler : IRequestHandler<DeleteSkuCommand, bool>
{
    private readonly ISkuRepository _skuRepository;
    private readonly IProductDbContext _dbContext;

    public DeleteSkuHandler(ISkuRepository skuRepository, IProductDbContext dbContext)
    {
        _skuRepository = skuRepository;
        _dbContext = dbContext;
    }

    public async Task<bool> Handle(DeleteSkuCommand request, CancellationToken cancellationToken)
    {
        var result = await _skuRepository.DeleteAsync(request.Id, cancellationToken);
        if (!result)
            throw new KeyNotFoundException($"SKU with ID '{request.Id}' not found.");

        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
