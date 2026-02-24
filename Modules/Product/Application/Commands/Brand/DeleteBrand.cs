using MediatR;
using Modules.Product.Application.Contracts;

namespace Modules.Product.Application.Commands;

public sealed record DeleteBrandCommand(Guid Id) : IRequest<bool>;

public sealed class DeleteBrandHandler(IBrandRepository brandRepository, IProductDbContext dbContext) : IRequestHandler<DeleteBrandCommand, bool>
{
    private readonly IBrandRepository _brandRepository = brandRepository;
    private readonly IProductDbContext _dbContext = dbContext;

    public async Task<bool> Handle(DeleteBrandCommand request, CancellationToken cancellationToken)
    {
        var result = await _brandRepository.DeleteAsync(request.Id, cancellationToken);
        if (!result)
            throw new KeyNotFoundException($"Brand with ID '{request.Id}' not found.");

        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
