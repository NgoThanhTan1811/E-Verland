using Modules.User.Application.Interfaces.Repositories;
using Modules.User.Application.DTOs.Response;
using MediatR;
using Modules.User.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Modules.User.Application.Validators;
using System.ComponentModel.DataAnnotations;
using Modules.User.Application.Mappings;

namespace Modules.User.Application.Commands;

public sealed record CreateAcountCommand(
    string Email,
    string Username,
    string Password
) : IRequest<AccountResDto>;

public sealed class CreateAccountHandler(IAccountRepository repo, IUserDbContext db) : IRequestHandler<CreateAcountCommand, AccountResDto>
{
    private readonly IAccountRepository _repo = repo;
    private readonly IUserDbContext _db = db;

    public async Task<AccountResDto> Handle(CreateAcountCommand request, CancellationToken ct)
    {
        var validationResult = AccountValidator.CreateAccount.Validate(new DTOs.Request.CreateAccountReqDto
        {
            Email = request.Email,
            Username = request.Username,
            Password = request.Password
        });

        if (validationResult != ValidationResult.Success)
            throw new ValidationException(validationResult?.ErrorMessage);

        var email = request.Email.Trim().ToLowerInvariant();
        var username = request.Username.Trim().ToLowerInvariant();

        var existsMail = await _repo.ExistsByEmailAsync(email, ct);
        var existsUsername = await _repo.ExistsByUsernameAsync(username, ct);

        if (existsMail || existsUsername)
            throw new InvalidOperationException("Account already exists with given email or username.");

        var password = BCrypt.Net.BCrypt.HashPassword(request.Password.Trim());
        var entity = new Account(email, username, password);

        await _repo.CreateAsync(entity, ct);

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            throw new InvalidOperationException("Account creation failed due to database constraint.");
        }

        return entity.ToResDto();
    }
}