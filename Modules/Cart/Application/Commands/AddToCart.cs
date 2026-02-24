using AutoMapper;
using MediatR;
using Modules.Cart.Application.Contracts;
using Modules.Cart.Application.DTOs.Request;
using Modules.Cart.Application.DTOs.Response;

namespace Modules.Cart.Application.Commands;

public sealed record AddToCartCommand(Guid UserId, AddToCartRequestDto Request) : IRequest<CartResponseDto>;

public sealed class AddToCartHandler(
    ICartRepository cartRepository,
    ICartItemRepository cartItemRepository,
    ICartDbContext dbContext,
    IMapper mapper) : IRequestHandler<AddToCartCommand, CartResponseDto>
{
    private readonly ICartRepository _cartRepository = cartRepository;
    private readonly ICartItemRepository _cartItemRepository = cartItemRepository;
    private readonly ICartDbContext _dbContext = dbContext;
    private readonly IMapper _mapper = mapper;

    public async Task<CartResponseDto> Handle(AddToCartCommand request, CancellationToken cancellationToken)
    {
        var cart = await _cartRepository.GetByUserIdAsync(request.UserId, cancellationToken);

        if (cart == null)
        {
            cart = new Domain.Cart
            {
                UserId = request.UserId,
                TotalItems = 0,
                Items = []
            };
            await _cartRepository.CreateAsync(cart, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        var existingItem = await _cartItemRepository.GetByCartIdAndSkuIdAsync(
            cart.Id, request.Request.SkuId, cancellationToken);

        if (existingItem != null)
        {
            existingItem.Quantity += request.Request.Quantity;
            await _cartItemRepository.UpdateAsync(existingItem, cancellationToken);
        }
        else
        {
            var cartItem = new Domain.CartItem
            {
                CartId = cart.Id,
                ProductId = request.Request.ProductId,
                SkuId = request.Request.SkuId,
                Quantity = request.Request.Quantity,
                ProductName = request.Request.ProductName,
                ProductImage = request.Request.ProductImage,
                SkuValue = request.Request.SkuValue
            };
            await _cartItemRepository.CreateAsync(cartItem, cancellationToken);
            cart.Items.Add(cartItem);
        }

        cart.TotalItems = cart.Items.Sum(x => x.Quantity);
        await _cartRepository.UpdateAsync(cart, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var updatedCart = await _cartRepository.GetByIdAsync(cart.Id, cancellationToken)
            ?? throw new InvalidOperationException("Failed to retrieve cart after update");

        return _mapper.Map<CartResponseDto>(updatedCart);
    }
}
