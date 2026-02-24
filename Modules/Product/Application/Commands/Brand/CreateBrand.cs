using MediatR;
using Modules.Product.Application.Contracts;
using Modules.Product.Application.DTOs.Request;
using Modules.Product.Application.DTOs.Response;

namespace Modules.Product.Application.Commands;

public sealed record CreateBrandCommand(CreateBrandRequestDto Request) : IRequest<BrandDetailDto>;

public sealed class CreateBrandHandler(IBrandRepository brandRepository, IProductDbContext dbContext) : IRequestHandler<CreateBrandCommand, BrandDetailDto>
{
    private readonly IBrandRepository _brandRepository = brandRepository;
    private readonly IProductDbContext _dbContext = dbContext;

    public async Task<BrandDetailDto> Handle(CreateBrandCommand request, CancellationToken cancellationToken)
    {
        var existingBrand = await _brandRepository.GetByNameAsync(request.Request.Name, cancellationToken);
        if (existingBrand != null)
            throw new InvalidOperationException($"Brand with name '{request.Request.Name}' already exists.");

        var brand = new Domain.Brand
        {
            Name = request.Request.Name,
            Slug = request.Request.Slug
        };

        await _brandRepository.CreateAsync(brand, cancellationToken);
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
