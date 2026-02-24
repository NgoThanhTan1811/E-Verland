using Modules.User.Application.Interfaces.Repositories;
using Modules.User.Application.DTOs.Response;
using MediatR;
using Modules.User.Domain.Entities;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Modules.User.Domain.Enums;
using System.ComponentModel.DataAnnotations;
using Modules.User.Application.Validators;

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

public sealed class CreateAddressHandler(IAddressRepository repo, IMapper mapper, IUserDbContext db) : IRequestHandler<CreateAddressCommand, AddressResDto>
{
    private readonly IAddressRepository _repo = repo;
    private readonly IMapper _mapper = mapper;
    private readonly IUserDbContext _db = db;

    public async Task<AddressResDto> Handle(CreateAddressCommand request, CancellationToken ct)
    {
        var validationResult = AddressValidator.CreateAddress.Validate(new DTOs.Request.CreateAddressReqDto
        {
            Label = request.Label,
            City = request.City,
            Province = request.Province,
            District = request.District,
            Ward = request.Ward,
            Street = request.Street,
            Detail = request.Detail
        });

        if (validationResult != ValidationResult.Success)
            throw new ValidationException(validationResult?.ErrorMessage);

        if (request.IsDefault)
        {
            await _repo.UnsetDefaultAsync(request.AccountId, ct);
        }

        var entity = new Address(
            request.AccountId,
            request.Label,
            request.City,
            request.Province,
            request.District,
            request.Ward,
            request.Street,
            request.Detail,
            request.IsDefault
        );

        await _repo.CreateAsync(entity, ct);

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            throw new InvalidOperationException("Address creation failed due to database constraint.");
        }

        return _mapper.Map<AddressResDto>(entity);
    }
}