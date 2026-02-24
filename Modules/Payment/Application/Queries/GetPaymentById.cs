using AutoMapper;
using MediatR;
using Modules.Payment.Application.Contracts;
using Modules.Payment.Application.DTOs.Response;

namespace Modules.Payment.Application.Queries;

public sealed record GetPaymentByIdQuery(Guid PaymentId) : IRequest<PaymentResponseDto>;

public sealed class GetPaymentByIdHandler(IPaymentRepository repo, IMapper mapper) : IRequestHandler<GetPaymentByIdQuery, PaymentResponseDto>
{
    private readonly IPaymentRepository _repo = repo;
    private readonly IMapper _mapper = mapper;

    public async Task<PaymentResponseDto> Handle(GetPaymentByIdQuery request, CancellationToken ct)
    {
        var payment = await _repo.GetByIdAsync(request.PaymentId, ct)
            ?? throw new KeyNotFoundException("Payment not found");

        return _mapper.Map<PaymentResponseDto>(payment);
    }
}
