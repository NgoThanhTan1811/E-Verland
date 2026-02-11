using Modules.User.Domain.Entities;
using Modules.User.Application.DTOs.Request;
using Modules.User.Application.DTOs.Response;

namespace Modules.User.Application.Mappings
{
    public class AccountMapping : AutoMapper.Profile
    {
        public AccountMapping()
        {
            CreateMap<CreateAccountReqDto, Account>();
            CreateMap<UpdateAccountReqDto, Account>()
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            CreateMap<Account, AccountResDto>();
        }
    }
}