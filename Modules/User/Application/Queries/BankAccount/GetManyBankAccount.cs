
using AutoMapper;
using MediatR;
using Modules.User.Application.DTOs.Response;
using Modules.User.Application.Interfaces.Repositories;
using SharedKernel.Pagination;

namespace Modules.User.Application.Queries.BankAccount
{
    public sealed record GetManyBankAccountByQuery(PagingFilter Filter) : IRequest<PageResult<BankAccountResDto>>;

    public sealed class GetManyBankAccountHandler(IBankAccountRepository repo, IMapper mapper) : IRequestHandler<GetManyBankAccountByQuery, PageResult<BankAccountResDto>>
    {
        private readonly IBankAccountRepository _repo = repo;
        private readonly IMapper _mapper = mapper;

        public async Task<PageResult<BankAccountResDto>> Handle(GetManyBankAccountByQuery request, CancellationToken ct)
        {
            var result = await _repo.GetPagedAsync(request.Filter, ct)
                ?? throw new KeyNotFoundException("BankAccount not found.");

            return new PageResult<BankAccountResDto>
            {
                Items = _mapper.Map<IReadOnlyCollection<BankAccountResDto>>(result.Items),
                TotalItems = result.TotalItems,
                Page = result.Page,
                Limit = result.Limit
            };
        }
    }

}