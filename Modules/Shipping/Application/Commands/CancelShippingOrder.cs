using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using Modules.Shipping.Application.Contracts;
using Modules.Shipping.Application.DTOs.Response;
using Modules.Shipping.Domain;

namespace Modules.Shipping.Application.Commands;

public sealed record CancelShippingOrderCommand(Guid OrderId) : IRequest<ShippingOrderResponseDto>;

/// <summary>
/// Cancels a shipping order locally. GHN is not called because shipping is managed
/// in draft/manual mode — no live shipper order exists to cancel.
/// </summary>
public sealed class CancelShippingOrderHandler(
    IShippingRepository repo,
    IShippingDbContext db,
    IMapper mapper,
    ILogger<CancelShippingOrderHandler> logger)
    : IRequestHandler<CancelShippingOrderCommand, ShippingOrderResponseDto>
{
    private readonly IShippingRepository _repo = repo;
    private readonly IShippingDbContext _db = db;
    private readonly IMapper _mapper = mapper;
    private readonly ILogger<CancelShippingOrderHandler> _logger = logger;

    public async Task<ShippingOrderResponseDto> Handle(CancelShippingOrderCommand request, CancellationToken ct)
    {
        var shipping = await _repo.GetByOrderIdAsync(request.OrderId, ct)
            ?? throw new KeyNotFoundException("Shipping order not found");

        if (shipping.Status == ShippingStatus.Canceled)
        {
            return _mapper.Map<ShippingOrderResponseDto>(shipping);
        }

        shipping.Status = ShippingStatus.Canceled;
        shipping.ProviderStatus = "canceled";
        shipping.LastSyncedAt = DateTime.UtcNow;

        await _repo.UpdateAsync(shipping, ct);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Shipping order {OrderId} canceled (manual, no GHN call)", shipping.OrderId);

        return _mapper.Map<ShippingOrderResponseDto>(shipping);
    }
}
