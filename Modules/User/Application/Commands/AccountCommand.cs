
using Modules.User.Application.Interfaces.Repositories;
using Modules.User.Application.DTOs.Response;
using MediatR;
using Modules.User.Domain.Entities;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace Modules.User.Application.Commands;

public sealed record CreateAcountCommand(
    string Email,
    string Username,
    string Password

) : IRequest<AccountResDto>;

public sealed class CreateAccountHandler : IRequestHandler<CreateAcountCommand, AccountResDto>
{
    private readonly IAccountRepository _repo;
    private readonly IMapper _mapper;
    private readonly IUserDbContext _db;

    public CreateAccountHandler(IAccountRepository repo, IMapper mapper, IUserDbContext db)
    {
        _repo = repo;
        _mapper = mapper;
        _db = db;
    }

    public async Task<AccountResDto> Handle(CreateAcountCommand request, CancellationToken ct)
    {
        var email = (request.Email ?? "").Trim().ToLowerInvariant();
        var username = (request.Username ?? "").Trim().ToLowerInvariant();
        var password = BCrypt.Net.BCrypt.HashPassword((request.Password ?? "").Trim());

        var existsMail = await _repo.ExistsByEmailAsync(email, ct);
        var exiistsUsername = await _repo.ExistsByUsernameAsync(username, ct);

        if (existsMail || exiistsUsername) throw new InvalidOperationException("Account already exists with given email or username.");

        var entity = new Account(email, username, password);

        await _repo.CreateAsync(entity, ct);

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            throw new InvalidOperationException("Account already exists with given email or username.");
        }
        return _mapper.Map<AccountResDto>(entity);

    }
}