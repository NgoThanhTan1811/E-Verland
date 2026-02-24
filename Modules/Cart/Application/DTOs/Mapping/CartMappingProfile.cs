using AutoMapper;
using Modules.Cart.Application.DTOs.Response;
using Modules.Cart.Domain;

namespace Modules.Cart.Application.DTOs.Mapping;

public class CartMappingProfile : Profile
{
    public CartMappingProfile()
    {
        CreateMap<Domain.Cart, CartResponseDto>();
    }
}
