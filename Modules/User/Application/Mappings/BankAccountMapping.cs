using Modules.User.Domain.Entities;
using Modules.User.Application.DTOs.Request;
using Modules.User.Application.DTOs.Response;

namespace Modules.User.Application.Mappings
{
    public class BankAccountMapping : AutoMapper.Profile
    {
        public BankAccountMapping()
        {
            CreateMap<CreateBankAccountReqDto, BankAccount>();
            CreateMap<UpdateBankAccountReqDto, BankAccount>()
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            CreateMap<BankAccount, BankAccountResDto>();
        }


    }
}