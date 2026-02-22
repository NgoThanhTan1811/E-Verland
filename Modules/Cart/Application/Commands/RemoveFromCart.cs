using AutoMapper;
using MediatR;
using Modules.Cart.Application.Contracts;
using Modules.Cart.Application.DTOs.Response;

namespace Modules.Cart.Application.Commands;

public sealed record RemoveFromCartCommand(Guid CartItemId) : IRequest<CartResponseDto>;

public sealed class RemoveFromCartHandler : IRequestHandler<RemoveFromCartCommand, CartResponseDto>
{
    private readonly ICartRepository _cartRepository;
    private readonly ICartItemRepository _cartItemRepository;
    private readonly ICartDbContext _dbContext;
    private readonly IMapper _mapper;

    public RemoveFromCartHandler(
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

    public async Task<CartResponseDto> Handle(RemoveFromCartCommand request, CancellationToken cancellationToken)
    {
        var cartItem = await _cartItemRepository.GetByIdAsync(request.CartItemId, cancellationToken)
            ?? throw new KeyNotFoundException($"Cart item with ID '{request.CartItemId}' not found.");

        var cart = await _cartRepository.GetByIdAsync(cartItem.CartId, cancellationToken)
            ?? throw new InvalidOperationException($"Cart for item '{request.CartItemId}' not found.");

        await _cartItemRepository.DeleteAsync(request.CartItemId, cancellationToken);

        cart.Items.RemoveAll(x => x.Id == request.CartItemId);
        cart.TotalItems = cart.Items.Sum(x => x.Quantity);

        if (cart.Items.Count == 0)
        {
            cart.TotalItems = 0;
        }

        await _cartRepository.UpdateAsync(cart, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var updatedCart = await _cartRepository.GetByIdAsync(cart.Id, cancellationToken)
            ?? throw new InvalidOperationException("Failed to retrieve cart after update");

        return _mapper.Map<CartResponseDto>(updatedCart);
    }
}
