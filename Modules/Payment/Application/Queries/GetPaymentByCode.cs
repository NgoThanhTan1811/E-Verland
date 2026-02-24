using AutoMapper;
using MediatR;
using Modules.Payment.Application.Contracts;
using Modules.Payment.Application.DTOs.Response;

namespace Modules.Payment.Application.Queries;

public sealed record GetPaymentByCodeQuery(string Code) : IRequest<PaymentResponseDto?>;

public sealed class GetPaymentByCodeHandler(IPaymentRepository repo, IMapper mapper) : IRequestHandler<GetPaymentByCodeQuery, PaymentResponseDto?>
{
    private readonly IPaymentRepository _repo = repo;
    private readonly IMapper _mapper = mapper;

    public async Task<PaymentResponseDto?> Handle(GetPaymentByCodeQuery request, CancellationToken ct)
    {
        var payment = await _repo.GetByPaymentCode(request.Code, ct);

        return payment == null ? null : _mapper.Map<PaymentResponseDto>(payment);
    }
}
