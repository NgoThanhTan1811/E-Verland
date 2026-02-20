using MediatR;
using Modules.Product.Application.Abtracsts;

namespace Modules.Product.Application.Commands;

public sealed record DeleteCategoryCommand(Guid Id) : IRequest<bool>;

public sealed class DeleteCategoryHandler : IRequestHandler<DeleteCategoryCommand, bool>
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IProductDbContext _dbContext;

    public DeleteCategoryHandler(ICategoryRepository categoryRepository, IProductDbContext dbContext)
    {
        _categoryRepository = categoryRepository;
        _dbContext = dbContext;
    }

    public async Task<bool> Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
    {
        var result = await _categoryRepository.DeleteAsync(request.Id, cancellationToken);
        if (!result)
            throw new KeyNotFoundException($"Category with ID '{request.Id}' not found.");

        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
