using AutoMapper;
using MediatR;
using Modules.Payment.Application.Contracts;
using Modules.Payment.Application.DTOs.Response;

namespace Modules.Payment.Application.Queries;

public sealed record GetUserPaymentsQuery(Guid UserId) : IRequest<List<PaymentOverviewResponseDto>>;

public sealed class GetUserPaymentsHandler(IPaymentRepository repo, IMapper mapper) : IRequestHandler<GetUserPaymentsQuery, List<PaymentOverviewResponseDto>>
{
    private readonly IPaymentRepository _repo = repo;
    private readonly IMapper _mapper = mapper;

    public async Task<List<PaymentOverviewResponseDto>> Handle(GetUserPaymentsQuery request, CancellationToken ct)
    {
        var payments = await _repo.GetByUserIdAsync(request.UserId, ct);

        return _mapper.Map<List<PaymentOverviewResponseDto>>(payments);
    }
}
