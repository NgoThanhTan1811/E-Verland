using Modules.User.Domain.Entities;
using Modules.User.Application.DTOs.Request;
using Modules.User.Application.DTOs.Response;

namespace Modules.User.Application.Mappings
{
    public class AddressMapping : AutoMapper.Profile
    {   
        public AddressMapping()
        {
            CreateMap<CreateAddressReqDto, Address>();
            CreateMap<UpdateAddressReqDto, Address>()
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            CreateMap<Address, AddressResDto>();
        }
        
        
    }
}