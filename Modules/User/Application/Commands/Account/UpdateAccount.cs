using Modules.User.Application.Interfaces.Repositories;
using Modules.User.Application.DTOs.Response;
using MediatR;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Modules.User.Domain.Enums;
using System.ComponentModel.DataAnnotations;
using Modules.User.Application.Validators;

namespace Modules.User.Application.Commands
{
    public sealed record UpdateAccountCommand(
        Guid AccountId,
        string? Username,
        string? Password,
        RoleUser? Role,
        StatusUser? Status
    ) : IRequest<AccountResDto>;

    public sealed class UpdateAccountHandler(IAccountRepository repo, IMapper mapper, IUserDbContext db)
                : IRequestHandler<UpdateAccountCommand, AccountResDto>
    {
        private readonly IAccountRepository _repo = repo;
        private readonly IUserDbContext _db = db;
        private readonly IMapper _mapper = mapper;

        public async Task<AccountResDto> Handle(UpdateAccountCommand request, CancellationToken ct)
        {
            var entity = await _repo.GetByIdAsync(request.AccountId, ct)
                ?? throw new KeyNotFoundException("Account not found.");

            var validationResult = AccountValidator.UpdateAccount.Validate(new DTOs.Request.UpdateAccountReqDto
            {
                Username = request.Username,
                Password = request.Password,
                Role = request.Role,
                Status = request.Status
            });

            if (validationResult != ValidationResult.Success)
                throw new ValidationException(validationResult?.ErrorMessage);

            if (request.Username is not null)
            {
                var existingAccount = await _repo.GetByUsernameAsync(request.Username.Trim(), ct);
                if (existingAccount != null && existingAccount.Id != request.AccountId)
                    throw new InvalidOperationException("Username already exists.");

                entity.Username = request.Username.Trim();
                entity.NormalizedUsername = entity.Username.ToUpperInvariant();
            }

            if (request.Password is not null)
            {
                entity.Password = BCrypt.Net.BCrypt.HashPassword(request.Password);
            }

            if (request.Role.HasValue)
            {
                if (!Enum.IsDefined(request.Role.Value))
                    throw new ArgumentException("Invalid role.", nameof(request.Role));
                entity.Role = request.Role.Value;
            }

            if (request.Status.HasValue)
            {
                if (!Enum.IsDefined(request.Status.Value))
                    throw new ArgumentException("Invalid status.", nameof(request.Status));
                entity.Status = request.Status.Value;
            }

            try
            {
                await _db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException)
            {
                throw new InvalidOperationException("Update failed due to a database constraint.");
            }

            return _mapper.Map<AccountResDto>(entity);
        }
    }
}