using MediatR;
using Modules.Shipping.Application.Contracts;
using Modules.Shipping.Application.DTOs.Request;
using Modules.Shipping.Application.DTOs.Response;

namespace Modules.Shipping.Application.Queries;

public sealed record CalculateShippingFeeQuery(CalculateShippingFeeRequestDto Payload) : IRequest<ShippingFeeResponseDto>;

/// <summary>
/// Returns a fixed mock shipping fee. GHN API is not called because shipping is managed
/// in draft/manual mode — the fee is for display purposes only.
/// </summary>
public sealed class CalculateShippingFeeHandler
    : IRequestHandler<CalculateShippingFeeQuery, ShippingFeeResponseDto>
{
    // Fixed fee shown to the customer while in draft mode.
    private const decimal MockServiceFee = 30_000m;

    public Task<ShippingFeeResponseDto> Handle(CalculateShippingFeeQuery request, CancellationToken ct)
    {
        var result = new ShippingFeeResponseDto(
            Total: MockServiceFee,
            ServiceFee: MockServiceFee,
            InsuranceFee: 0,
            PickStationFee: 0,
            CouponValue: 0,
            R2SFee: 0,
            DocumentReturn: 0,
            DoubleCheck: 0,
            CodFee: 0,
            PickRemoteAreasFee: 0,
            DeliverRemoteAreasFee: 0,
            CodFailedFee: 0
        );

        return Task.FromResult(result);
    }
}
