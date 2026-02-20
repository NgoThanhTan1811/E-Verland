using MediatR;
using Modules.Product.Application.Abtracsts;
using Modules.Product.Application.DTOs.Request;
using Modules.Product.Application.DTOs.Response;

namespace Modules.Product.Application.Commands;

public sealed record UpdateBrandCommand(Guid Id, UpdateBrandRequestDto Request) : IRequest<BrandDetailDto>;

public sealed class UpdateBrandHandler : IRequestHandler<UpdateBrandCommand, BrandDetailDto>
{
    private readonly IBrandRepository _brandRepository;
    private readonly IProductDbContext _dbContext;

    public UpdateBrandHandler(IBrandRepository brandRepository, IProductDbContext dbContext)
    {
        _brandRepository = brandRepository;
        _dbContext = dbContext;
    }

    public async Task<BrandDetailDto> Handle(UpdateBrandCommand request, CancellationToken cancellationToken)
    {
        var brand = await _brandRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Brand with ID '{request.Id}' not found.");

        if (brand.Name != request.Request.Name)
        {
            var existingBrand = await _brandRepository.GetByNameAsync(request.Request.Name, cancellationToken);
            if (existingBrand != null)
                throw new InvalidOperationException($"Brand with name '{request.Request.Name}' already exists.");
        }

        brand.Name = request.Request.Name;
        brand.Slug = request.Request.Slug;

        await _brandRepository.UpdateAsync(brand, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new BrandDetailDto
        {
            Id = brand.Id,
            Name = brand.Name,
            Slug = brand.Slug,
            CreatedAt = brand.CreatedAt,
            UpdatedAt = brand.UpdatedAt
        };
    }
}
