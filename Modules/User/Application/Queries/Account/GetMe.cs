using MediatR;
using Modules.User.Application.DTOs.Response;
using Modules.User.Application.Interfaces.Repositories;
using Modules.User.Application.Mappings;

namespace Modules.User.Application.Queries.Account;

public sealed record GetMeQuery(Guid AccountId) : IRequest<AccountMeResDto>;

public sealed class GetMeHandler(
    IAccountRepository accountRepository,
    IProfileRepository profileRepository,
    IAddressRepository addressRepository,
    IBankAccountRepository bankAccountRepository) : IRequestHandler<GetMeQuery, AccountMeResDto>
{
    private readonly IAccountRepository _accountRepository = accountRepository;
    private readonly IProfileRepository _profileRepository = profileRepository;
    private readonly IAddressRepository _addressRepository = addressRepository;
    private readonly IBankAccountRepository _bankAccountRepository = bankAccountRepository;

    public async Task<AccountMeResDto> Handle(GetMeQuery request, CancellationToken ct)
    {
        var account = await _accountRepository.GetByIdAsync(request.AccountId, ct)
            ?? throw new KeyNotFoundException("Account not found.");

        var profile = await _profileRepository.GetByAccountIdAsync(account.Id, ct);

        IReadOnlyList<AddressResDto> addresses = [];
        IReadOnlyList<BankAccountResDto> bankAccounts = [];

        if (profile is not null)
        {
            var addressEntities = await _addressRepository.GetByProfileIdAsync(profile.Id, ct);
            var bankAccountEntities = await _bankAccountRepository.GetByProfileIdAsync(profile.Id, ct);

            addresses = addressEntities.Select(x => x.ToResDto()).ToList();
            bankAccounts = bankAccountEntities.Select(x => x.ToResDto()).ToList();
        }

        return new AccountMeResDto
        {
            Account = account.ToResDto(),
            Profile = profile?.ToResDto(),
            Addresses = addresses,
            BankAccounts = bankAccounts
        };
    }
}