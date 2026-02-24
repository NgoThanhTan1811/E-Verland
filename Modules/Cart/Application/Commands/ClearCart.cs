using AutoMapper;
using MediatR;
using Modules.Cart.Application.Contracts;
using Modules.Cart.Application.DTOs.Response;

namespace Modules.Cart.Application.Commands;

public sealed record ClearCartCommand(Guid CartId) : IRequest<CartResponseDto>;

public sealed class ClearCartHandler(
    ICartRepository cartRepository,
    ICartItemRepository cartItemRepository,
    ICartDbContext dbContext,
    IMapper mapper) : IRequestHandler<ClearCartCommand, CartResponseDto>
{
    private readonly ICartRepository _cartRepository = cartRepository;
    private readonly ICartItemRepository _cartItemRepository = cartItemRepository;
    private readonly ICartDbContext _dbContext = dbContext;
    private readonly IMapper _mapper = mapper;

    public async Task<CartResponseDto> Handle(ClearCartCommand request, CancellationToken cancellationToken)
    {
        var cart = await _cartRepository.GetByIdAsync(request.CartId, cancellationToken)
            ?? throw new KeyNotFoundException($"Cart with ID '{request.CartId}' not found.");

        var items = await _cartItemRepository.GetByCartIdAsync(request.CartId, cancellationToken);

        foreach (var item in items)
        {
            await _cartItemRepository.DeleteAsync(item.Id, cancellationToken);
        }

        cart.Items.Clear();
        cart.TotalItems = 0;

        await _cartRepository.UpdateAsync(cart, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var updatedCart = await _cartRepository.GetByIdAsync(cart.Id, cancellationToken)
            ?? throw new InvalidOperationException("Failed to retrieve cart after clearing");

        return _mapper.Map<CartResponseDto>(updatedCart);
    }
}
