using AutoMapper;
using MediatR;
using Modules.Cart.Application.Contracts;
using Modules.Cart.Application.DTOs.Response;

namespace Modules.Cart.Application.Queries;

public sealed record GetUserCartQuery(Guid UserId) : IRequest<CartResponseDto?>;

public sealed class GetUserCartHandler : IRequestHandler<GetUserCartQuery, CartResponseDto?>
{
    private readonly ICartRepository _cartRepository;
    private readonly IMapper _mapper;

    public GetUserCartHandler(ICartRepository cartRepository, IMapper mapper)
    {
        _cartRepository = cartRepository;
        _mapper = mapper;
    }

    public async Task<CartResponseDto?> Handle(GetUserCartQuery request, CancellationToken cancellationToken)
    {
        var cart = await _cartRepository.GetByUserIdAsync(request.UserId, cancellationToken);

        if (cart == null)
            return null;

        return _mapper.Map<CartResponseDto>(cart);
    }
}
