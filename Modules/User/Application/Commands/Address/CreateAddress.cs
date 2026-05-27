using Modules.User.Application.Interfaces.Repositories;
using Modules.User.Application.DTOs.Response;
using MediatR;
using Modules.User.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Modules.User.Domain.Enums;
using System.ComponentModel.DataAnnotations;
using Modules.User.Application.Validators;
using Modules.User.Application.Mappings;

namespace Modules.User.Application.Commands;

public sealed record CreateAddressCommand(
    Guid AccountId,
    string Street,
    string City,
    string Ward,
    string Detail,
    string District,
    string Province,
    int ProvinceId,
    int DistrictId,
    string WardCode,
    LableAddress Label,
    bool IsDefault
) : IRequest<AddressResDto>;

public sealed class CreateAddressHandler(IAddressRepository repo, IUserDbContext db) : IRequestHandler<CreateAddressCommand, AddressResDto>
{
    private readonly IAddressRepository _repo = repo;
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
            Detail = request.Detail,
            ProvinceId = request.ProvinceId,
            DistrictId = request.DistrictId,
            WardCode = request.WardCode
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
            request.IsDefault,
            request.ProvinceId,
            request.DistrictId,
            request.WardCode
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

        return entity.ToResDto();
    }
}