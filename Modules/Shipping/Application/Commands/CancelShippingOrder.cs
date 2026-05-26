using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using Modules.Shipping.Application.Contracts;
using Modules.Shipping.Application.DTOs.External;
using Modules.Shipping.Application.DTOs.Response;
using Modules.Shipping.Domain;

namespace Modules.Shipping.Application.Commands;

public sealed record CancelShippingOrderCommand(Guid OrderId) : IRequest<ShippingOrderResponseDto>;

public sealed class CancelShippingOrderHandler(
    IShippingRepository repo,
    IShippingDbContext db,
    IGhnClient ghnClient,
    IMapper mapper,
    ILogger<CancelShippingOrderHandler> logger)
    : IRequestHandler<CancelShippingOrderCommand, ShippingOrderResponseDto>
{
    private readonly IShippingRepository _repo = repo;
    private readonly IShippingDbContext _db = db;
    private readonly IGhnClient _ghnClient = ghnClient;
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

        if (!string.IsNullOrWhiteSpace(shipping.ProviderOrderCode))
        {
            var response = await _ghnClient.CancelOrderAsync(
                new GhnCancelRequest([shipping.ProviderOrderCode]), ct);

            var result = response.Data?.FirstOrDefault();
            if (result is not null && !result.Result)
            {
                _logger.LogWarning("GHN cancel failed for {OrderCode}: {Message}",
                    result.OrderCode, result.Message);
            }
        }

        shipping.Status = ShippingStatus.Canceled;
        shipping.ProviderStatus = "canceled";
        shipping.LastSyncedAt = DateTime.UtcNow;

        await _repo.UpdateAsync(shipping, ct);
        await _db.SaveChangesAsync(ct);

        return _mapper.Map<ShippingOrderResponseDto>(shipping);
    }
}
