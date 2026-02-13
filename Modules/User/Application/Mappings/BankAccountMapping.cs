using Modules.User.Domain.Entities;
using Modules.User.Application.DTOs.Response;
using AutoMapper;

namespace Modules.User.Application.Mappings
{
    public class BankAccountMapping : AutoMapper.Profile
    {
        public BankAccountMapping()
        {
            CreateMap<BankAccount, BankAccountResDto>();
        }
    }
}