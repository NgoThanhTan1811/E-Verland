using Modules.User.Domain.Entities;
using Modules.User.Application.DTOs.Response;

namespace Modules.User.Application.Mappings
{
    public class AddressMapping : AutoMapper.Profile
    {
        public AddressMapping()
        {
            CreateMap<Address, AddressResDto>();
        }
    }
}