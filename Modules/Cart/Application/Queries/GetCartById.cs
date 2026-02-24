using AutoMapper;
using MediatR;
using Modules.Cart.Application.Contracts;
using Modules.Cart.Application.DTOs.Response;

namespace Modules.Cart.Application.Queries;

public sealed record GetCartByIdQuery(Guid CartId) : IRequest<CartResponseDto?>;

public sealed class GetCartByIdHandler(ICartRepository cartRepository, IMapper mapper) : IRequestHandler<GetCartByIdQuery, CartResponseDto?>
{
    private readonly ICartRepository _cartRepository = cartRepository;
    private readonly IMapper _mapper = mapper;

    public async Task<CartResponseDto?> Handle(GetCartByIdQuery request, CancellationToken cancellationToken)
    {
        var cart = await _cartRepository.GetByIdAsync(request.CartId, cancellationToken);

        if (cart == null)
            return null;

        return _mapper.Map<CartResponseDto>(cart);
    }
}
