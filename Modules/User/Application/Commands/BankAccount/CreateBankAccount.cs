using Modules.User.Application.Interfaces.Repositories;
using Modules.User.Application.DTOs.Response;
using Modules.User.Domain.Entities;
using MediatR;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using Modules.User.Application.Validators;

namespace Modules.User.Application.Commands
{
    public sealed record CreateBankAccountCommand(
        Guid ProfileId,
        string BankName,
        string BankCode,
        string AccountNumber,
        string AccountHolderName
    ) : IRequest<BankAccountResDto>;

    public sealed class CreateBankAccountHandler(IBankAccountRepository repo, IMapper mapper, IUserDbContext db)
                : IRequestHandler<CreateBankAccountCommand, BankAccountResDto>
    {
        private readonly IBankAccountRepository _repo = repo;
        private readonly IUserDbContext _db = db;
        private readonly IMapper _mapper = mapper;

        public async Task<BankAccountResDto> Handle(CreateBankAccountCommand request, CancellationToken ct)
        {
            var validationResult = BankAccountValidator.CreateBankAccount.Validate(new DTOs.Request.CreateBankAccountReqDto
            {
                BankName = request.BankName,
                BankCode = request.BankCode,
                AccountNumber = request.AccountNumber,
                AccountHolder = request.AccountHolderName
            });

            if (validationResult != ValidationResult.Success)
                throw new ValidationException(validationResult?.ErrorMessage);

            var exists = await _repo.ExistsAccountNumberAsync(
                request.ProfileId,
                request.AccountNumber,
                Guid.Empty,
                ct
            );

            if (exists)
                throw new InvalidOperationException("Bank account number already exists for this profile.");

            var entity = new BankAccount(
                request.ProfileId,
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
                throw new InvalidOperationException("Bank account creation failed due to database constraint.");
            }

            return _mapper.Map<BankAccountResDto>(entity);
        }
    }
}