using Modules.User.Application.Validators;
using Modules.User.Application.DTOs;
using Modules.User.Application.Interfaces;
using Modules.User.Application.Interfaces.Repositories;
using Modules.User.Application.DTOs.Response;
using MediatR;
using Modules.User.Domain.Entities;
using AutoMapper;

namespace Modules.User.Application.Queries;
public sealed record CreateAcountQuery(
    string Email,
    string Username,
    string Password

) : IRequest<AccountResDto>;

public sealed class CreateAccountHandler : IRequestHandler<CreateAcountQuery, AccountResDto>
{
    private readonly IAccountRepository _repo;
    private readonly IMapper _mapper;

    public CreateAccountHandler(IAccountRepository repo, IMapper mapper)
    {
        _repo = repo;
        _mapper = mapper;
    }

    public async Task<AccountResDto> Handle(CreateAcountQuery request, CancellationToken ct)
    {
        var email = (request.Email ?? "").Trim();
        var username = (request.Username ?? "").Trim();
        var password = (request.Password ?? "").Trim();

        var existsMail = await _repo.ExistsByEmailAsync(email, ct);
        var exiistsUsername = await _repo.ExistsByUsernameAsync(username, ct);

        if (existsMail || exiistsUsername) throw new InvalidOperationException("Account already exists with given email or username.");

        var entity = _mapper.Map<Account>(request);

        await _repo.CreateAsync(entity, ct);

        return _mapper.Map<AccountResDto>(entity);

    }
}