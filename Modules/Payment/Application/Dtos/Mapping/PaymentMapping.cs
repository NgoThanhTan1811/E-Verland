using AutoMapper;
using Modules.Payment.Domain;
using Modules.Payment.Application.DTOs.Response;

namespace Modules.Payment.Application.DTOs.Mapping;

public sealed class PaymentMapping : Profile
{
    public PaymentMapping()
    {
        CreateMap<Modules.Payment.Domain.Payment, PaymentResponseDto>();
        CreateMap<Modules.Payment.Domain.Payment, PaymentOverviewResponseDto>();
    }
}