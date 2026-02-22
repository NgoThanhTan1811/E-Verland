using AutoMapper;
using Modules.Order.Application.DTOs.Response;
using Modules.Order.Domain;

namespace Modules.Order.Application.DTOs.Mapping;

public class OrderMapping : Profile
{
    public OrderMapping()
    {
        CreateMap<Domain.Order, OrderOverviewResponseDto>();

        CreateMap<Domain.Order, OrderDetailResponseDto>();

        CreateMap<OrderItem, OrderItemResponseDto>();

        CreateMap<ReceiverSnapshot, ReceiverResponseDto>();
    }
}
