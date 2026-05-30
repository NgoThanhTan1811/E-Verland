using AutoMapper;
using MediatR;
using Modules.Cart.Application.Contracts;
using Modules.Cart.Application.DTOs.Request;
using Modules.Cart.Application.DTOs.Response;
using Modules.Media.Application.Queries;
using Modules.Product.Application.Queries;

namespace Modules.Cart.Application.Commands;

public sealed record AddToCartCommand(Guid UserId, AddToCartRequestDto Request) : IRequest<CartResponseDto>;

public sealed class AddToCartHandler(
    ICartRepository cartRepository,
    ICartItemRepository cartItemRepository,
    IMediator mediator,
    ICartDbContext dbContext,
    IMapper mapper) : IRequestHandler<AddToCartCommand, CartResponseDto>
{
    private readonly ICartRepository _cartRepository = cartRepository;
    private readonly ICartItemRepository _cartItemRepository = cartItemRepository;
    private readonly IMediator _mediator = mediator;
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

        var existingItem = cart.Items.FirstOrDefault(x => x.SkuId == request.Request.SkuId);

        if (existingItem != null)
        {
            existingItem.Quantity += request.Request.Quantity;
            await _cartItemRepository.UpdateAsync(existingItem, cancellationToken);
        }
        else
        {
            var product = await _mediator.Send(new GetProductByIdQuery(request.Request.ProductId), cancellationToken)
                ?? throw new InvalidOperationException("Product not found");

            var selectedSku = product.Skus.FirstOrDefault(x => x.Id == request.Request.SkuId)
                ?? throw new InvalidOperationException("SKU does not belong to the selected product");

            string? productImage = null;
            var imagePath = product.ImageUrls.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(imagePath))
            {
                productImage = await _mediator.Send(new GetMediaUrlByPathQuery(imagePath), cancellationToken);
            }

            var skuValue = selectedSku.OptionValues.Count > 0
                ? string.Join(" / ", selectedSku.OptionValues.Select(option => $"{option.Key}: {option.Value}"))
                : selectedSku.SkuCode;

            var cartItem = new Domain.CartItem
            {
                CartId = cart.Id,
                ProductId = request.Request.ProductId,
                SkuId = request.Request.SkuId,
                Quantity = request.Request.Quantity,
                ProductName = product.Name,
                ProductImage = productImage,
                SkuValue = skuValue
            };
            cart.Items.Add(cartItem);
            await _cartItemRepository.CreateAsync(cartItem, cancellationToken);
        }

        cart.TotalItems = cart.Items.Sum(x => x.Quantity);
        await _cartRepository.UpdateAsync(cart, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var updatedCart = await _cartRepository.GetByIdAsync(cart.Id, cancellationToken)
            ?? throw new InvalidOperationException("Failed to retrieve cart after update");

        return _mapper.Map<CartResponseDto>(updatedCart);
    }
}
