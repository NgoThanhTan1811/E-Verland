using AutoMapper;
using Modules.Shipping.Application.DTOs.Response;
using Modules.Shipping.Domain;

namespace Modules.Shipping.Application.DTOs.Mapping;

public sealed class ShippingMapping : Profile
{
    public ShippingMapping()
    {
        CreateMap<ShippingOrder, ShippingOrderResponseDto>();
    }
}
