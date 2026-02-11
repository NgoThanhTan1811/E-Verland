
using Modules.User.Application.Interfaces.Repositories;
using Modules.User.Application.DTOs.Response;
using MediatR;
using Modules.User.Domain.Entities;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Modules.User.Domain.Enums;

namespace Modules.User.Application.Commands;

public sealed record CreateAddressCommand(
    Guid AccountId,
    string Street,
    string City,
    string Ward,
    string Detail,
    string District,
    string Province,
    LableAddress Label,
    bool IsDefault

) : IRequest<AddressResDto>;

public sealed class CreateAddressHandler : IRequestHandler<CreateAddressCommand, AddressResDto>
{
    private readonly IAddressRepository _repo;
    private readonly IMapper _mapper;
    private readonly IUserDbContext _db;

    public CreateAddressHandler(IAddressRepository repo, IMapper mapper, IUserDbContext db)
    {
        _repo = repo;
        _mapper = mapper;
        _db = db;
    }

    public async Task<AddressResDto> Handle(CreateAddressCommand request, CancellationToken ct)
    {
        var entity = new Address
        (
            request.AccountId,
            request.Label,
            request.City,
            request.District,
            request.Province,
            request.Ward,
            request.Detail,
            request.Street,
            request.IsDefault
        );

        
        await _repo.CreateAsync(entity, ct);
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            throw new InvalidOperationException("Account already exists with given email or username.");
        }
        return _mapper.Map<AddressResDto>(entity);

    }
}