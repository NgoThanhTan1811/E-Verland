using Modules.User.Domain.Entities;
using Modules.User.Application.DTOs.Response;

namespace Modules.User.Application.Mappings
{
    public class AccountMapping : AutoMapper.Profile
    {
        public AccountMapping()
        {
            CreateMap<Account, AccountResDto>();
        }
    }
}