
using Modules.User.Application.Interfaces.Repositories;
using Modules.User.Application.DTOs.Response;
using Modules.User.Domain.Entities;
using MediatR;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace Modules.User.Application.Commands
{
    public sealed record CreateBankAccountCommand(
        Guid AccountId,
        string BankName,
        string BankCode,
        string AccountNumber,
        string AccountHolderName
    ) : IRequest<BankAccountResDto>;

    public class BankAccountHandler : IRequestHandler<CreateBankAccountCommand, BankAccountResDto>
    {
        private readonly IBankAccountRepository _repo;
        private readonly IUserDbContext _db;
        private readonly IMapper _mapper;

        public BankAccountHandler(IBankAccountRepository repo, IMapper mapper, IUserDbContext db)
        {
            _repo = repo;
            _mapper = mapper;
            _db = db;
        }

        public async Task<BankAccountResDto> Handle(CreateBankAccountCommand request, CancellationToken ct)
        {
            var existsAccount = await _repo.ExistsAccountNumberAsync(request.AccountId, request.AccountNumber, ct);
            if (existsAccount) throw new InvalidOperationException("Bank account already exists for the given account ID.");

            var entity = new BankAccount
            (
                request.AccountId,
                request.BankName,
                request.BankCode,
                request.AccountNumber,
                request.AccountHolderName
            );

            await _repo.CreateAsync(entity, ct);

            try
            {
                await _db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException)
            {
                throw new InvalidOperationException("Bank account already exists for the given account.");
            }
            return _mapper.Map<BankAccountResDto>(entity);
        }
        
    }
}