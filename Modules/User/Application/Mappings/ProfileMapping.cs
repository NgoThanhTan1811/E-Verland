using Modules.User.Domain.Entities;
using Modules.User.Application.DTOs.Request;
using Modules.User.Application.DTOs.Response;

namespace Modules.User.Application.Mappings
{
    public class ProfileMapping : AutoMapper.Profile
    {
        public ProfileMapping()
        {
            CreateMap<CreateProfileReqDto, Profile>();
            CreateMap<UpdateProfileReqDto, Profile>()
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            CreateMap<Profile, ProfileResDto>();
        }


    }
}