using MediatR;
using Modules.Shipping.Application.Contracts;
using Modules.Shipping.Application.DTOs.External;
using Modules.Shipping.Application.DTOs.Request;
using Modules.Shipping.Application.DTOs.Response;

namespace Modules.Shipping.Application.Queries;

public sealed record CalculateShippingFeeQuery(CalculateShippingFeeRequestDto Payload) : IRequest<ShippingFeeResponseDto>;

public sealed class CalculateShippingFeeHandler(IGhnClient ghnClient)
    : IRequestHandler<CalculateShippingFeeQuery, ShippingFeeResponseDto>
{
    private readonly IGhnClient _ghnClient = ghnClient;

    public async Task<ShippingFeeResponseDto> Handle(CalculateShippingFeeQuery request, CancellationToken ct)
    {
        var payload = request.Payload;

        var items = payload.Items?.Select(i => new GhnFeeItem(
            i.Name,
            i.Quantity,
            i.Height,
            i.Weight,
            i.Length,
            i.Width)).ToList();

        var ghnRequest = new GhnFeeRequest(
            FromDistrictId: payload.FromDistrictId,
            FromWardCode: payload.FromWardCode,
            ServiceId: payload.ServiceId,
            ServiceTypeId: payload.ServiceTypeId,
            ToDistrictId: payload.ToDistrictId,
            ToWardCode: payload.ToWardCode,
            Height: payload.Height,
            Length: payload.Length,
            Weight: payload.Weight,
            Width: payload.Width,
            InsuranceValue: payload.InsuranceValue,
            CodFailedAmount: payload.CodFailedAmount,
            CodValue: payload.CodValue,
            Coupon: payload.Coupon,
            Items: items
        );

        var response = await _ghnClient.CalculateFeeAsync(ghnRequest, ct);
        if (response.Data is null)
        {
            throw new InvalidOperationException("GHN fee response is empty");
        }

        return new ShippingFeeResponseDto(
            response.Data.Total,
            response.Data.ServiceFee,
            response.Data.InsuranceFee,
            response.Data.PickStationFee,
            response.Data.CouponValue,
            response.Data.R2SFee,
            response.Data.DocumentReturn,
            response.Data.DoubleCheck,
            response.Data.CodFee,
            response.Data.PickRemoteAreasFee,
            response.Data.DeliverRemoteAreasFee,
            response.Data.CodFailedFee
        );
    }
}
