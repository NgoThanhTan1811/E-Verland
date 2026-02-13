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
    public sealed record UpdateBankAccountCommand(
        Guid BankAccountId,
        Guid ProfileId,
        string? BankName,
        string? BankCode,
        string? AccountNumber,
        string? AccountHolderName
    ) : IRequest<BankAccountResDto>;

    public sealed class UpdateBankAccountHandler
        : IRequestHandler<UpdateBankAccountCommand, BankAccountResDto>
    {
        private readonly IBankAccountRepository _repo;
        private readonly IUserDbContext _db;
        private readonly IMapper _mapper;

        public UpdateBankAccountHandler(IBankAccountRepository repo, IMapper mapper, IUserDbContext db)
        {
            _repo = repo;
            _mapper = mapper;
            _db = db;
        }

        public async Task<BankAccountResDto> Handle(UpdateBankAccountCommand request, CancellationToken ct)
        {
            var entity = await _repo.GetByIdForProfileAsync(request.BankAccountId, request.ProfileId, ct)
                ?? throw new KeyNotFoundException("Bank account not found.");

            var validationResult = BankAccountValidator.UpdateBankAccount.Validate(new DTOs.Request.UpdateBankAccountReqDto
            {
                BankName = request.BankName,
                BankCode = request.BankCode,
                AccountNumber = request.AccountNumber,
                AccountHolder = request.AccountHolderName
            });

            if (validationResult != ValidationResult.Success)
                throw new ValidationException(validationResult?.ErrorMessage);

            if (request.AccountNumber is not null)
            {
                var exists = await _repo.ExistsAccountNumberAsync(
                    request.ProfileId,
                    request.AccountNumber.Trim(),
                    request.BankAccountId,
                    ct
                );
                if (exists)
                    throw new InvalidOperationException("Account number already exists for this profile.");
            }

            entity.Update(request.BankName, request.BankCode, request.AccountNumber, request.AccountHolderName);

            try
            {
                await _db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException)
            {
                throw new InvalidOperationException("Update failed due to a database constraint.");
            }

            return _mapper.Map<BankAccountResDto>(entity);
        }
    }
}