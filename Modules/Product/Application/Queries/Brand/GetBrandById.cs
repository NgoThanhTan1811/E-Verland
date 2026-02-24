using MediatR;
using Modules.Product.Application.Contracts;
using Modules.Product.Application.DTOs.Response;

namespace Modules.Product.Application.Queries;

public sealed record GetBrandByIdQuery(Guid Id) : IRequest<BrandDetailDto?>;

public sealed class GetBrandByIdHandler(IBrandRepository brandRepository) : IRequestHandler<GetBrandByIdQuery, BrandDetailDto?>
{
    private readonly IBrandRepository _brandRepository = brandRepository;

    public async Task<BrandDetailDto?> Handle(GetBrandByIdQuery request, CancellationToken cancellationToken)
    {
        var brand = await _brandRepository.GetByIdAsync(request.Id, cancellationToken);
        if (brand == null)
            return null;

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
