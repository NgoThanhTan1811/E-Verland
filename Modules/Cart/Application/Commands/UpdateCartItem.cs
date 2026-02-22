using AutoMapper;
using MediatR;
using Modules.Cart.Application.Contracts;
using Modules.Cart.Application.DTOs.Request;
using Modules.Cart.Application.DTOs.Response;

namespace Modules.Cart.Application.Commands;

public sealed record UpdateCartItemCommand(UpdateCartItemRequestDto Request) : IRequest<CartResponseDto>;

public sealed class UpdateCartItemHandler : IRequestHandler<UpdateCartItemCommand, CartResponseDto>
{
    private readonly ICartRepository _cartRepository;
    private readonly ICartItemRepository _cartItemRepository;
    private readonly ICartDbContext _dbContext;
    private readonly IMapper _mapper;

    public UpdateCartItemHandler(
        ICartRepository cartRepository,
        ICartItemRepository cartItemRepository,
        ICartDbContext dbContext,
        IMapper mapper)
    {
        _cartRepository = cartRepository;
        _cartItemRepository = cartItemRepository;
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public async Task<CartResponseDto> Handle(UpdateCartItemCommand request, CancellationToken cancellationToken)
    {
        if (request.Request.Quantity <= 0)
            throw new InvalidOperationException("Quantity must be greater than 0");

        var cartItem = await _cartItemRepository.GetByIdAsync(request.Request.CartItemId, cancellationToken)
            ?? throw new KeyNotFoundException($"Cart item with ID '{request.Request.CartItemId}' not found.");

        var cart = await _cartRepository.GetByIdAsync(cartItem.CartId, cancellationToken)
            ?? throw new InvalidOperationException($"Cart for item '{request.Request.CartItemId}' not found.");

        cartItem.Quantity = request.Request.Quantity;
        await _cartItemRepository.UpdateAsync(cartItem, cancellationToken);

        cart.TotalItems = cart.Items.Sum(x => x.Quantity);
        await _cartRepository.UpdateAsync(cart, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var updatedCart = await _cartRepository.GetByIdAsync(cart.Id, cancellationToken)
            ?? throw new InvalidOperationException("Failed to retrieve cart after update");

        return _mapper.Map<CartResponseDto>(updatedCart);
    }
}
