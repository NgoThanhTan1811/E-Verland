using Modules.User.Domain.Entities;
using Modules.User.Application.DTOs.Response;

namespace Modules.User.Application.Mappings
{
    public class ProfileMapping : AutoMapper.Profile
    {
        public ProfileMapping()
        {
            CreateMap<Profile, ProfileResDto>();
        }
    }
}