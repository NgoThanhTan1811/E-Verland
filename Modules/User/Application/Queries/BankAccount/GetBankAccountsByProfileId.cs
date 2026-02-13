
using AutoMapper;
using MediatR;
using Modules.User.Application.DTOs.Response;
using Modules.User.Application.Interfaces.Repositories;

namespace Modules.User.Application.Queries.BankAccount
{
    public sealed record GetManyBankAccountsByProfileIdQuery(Guid ProfileId) : IRequest<IReadOnlyList<BankAccountResDto>>;

    public sealed class GetManyBankAccountsByProfileIdHandler : IRequestHandler<GetManyBankAccountsByProfileIdQuery, IReadOnlyList<BankAccountResDto>>
    {
        private readonly IBankAccountRepository _repo;
        private readonly IMapper _mapper;

        public GetManyBankAccountsByProfileIdHandler(IBankAccountRepository repo, IMapper mapper)
        {
            _repo = repo;
            _mapper = mapper;
        }

        public async Task<IReadOnlyList<BankAccountResDto>> Handle(GetManyBankAccountsByProfileIdQuery request, CancellationToken ct)
        {
            var entity = await _repo.GetByProfileIdAsync(request.ProfileId, ct)
                ?? throw new KeyNotFoundException("BankAccount not found.");

            return _mapper.Map<IReadOnlyList<BankAccountResDto>>(entity);
        }
    }

}