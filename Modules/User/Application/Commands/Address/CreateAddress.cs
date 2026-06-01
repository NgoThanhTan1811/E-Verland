using Modules.User.Application.Interfaces.Repositories;
using Modules.User.Application.DTOs.Response;
using Modules.User.Application.Interfaces.Services;
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
    string Detail,
    int ProvinceId,
    int DistrictId,
    int WardId,
    LableAddress Label,
    bool IsDefault
) : IRequest<AddressResDto>;

public sealed class CreateAddressHandler(IAddressRepository repo, IUserDbContext db, ILocationLookupService locations) : IRequestHandler<CreateAddressCommand, AddressResDto>
{
    private readonly IAddressRepository _repo = repo;
    private readonly IUserDbContext _db = db;
    private readonly ILocationLookupService _locations = locations;

    public async Task<AddressResDto> Handle(CreateAddressCommand request, CancellationToken ct)
    {
        var validationResult = AddressValidator.CreateAddress.Validate(new DTOs.Request.CreateAddressReqDto
        {
            Label = request.Label,
            Street = request.Street,
            Detail = request.Detail,
            ProvinceId = request.ProvinceId,
            DistrictId = request.DistrictId,
            WardId = request.WardId
        });

        if (validationResult != ValidationResult.Success)
            throw new ValidationException(validationResult?.ErrorMessage);

        var provinceName = await _locations.GetProvinceNameAsync(request.ProvinceId, ct)
            ?? throw new KeyNotFoundException("ProvinceId not found.");

        var districtName = await _locations.GetDistrictNameAsync(request.ProvinceId, request.DistrictId, ct)
            ?? throw new KeyNotFoundException("DistrictId not found.");

        var wardName = await _locations.GetWardNameAsync(request.ProvinceId, request.DistrictId, request.WardId, ct)
            ?? throw new KeyNotFoundException("WardId not found.");

        if (request.IsDefault)
        {
            await _repo.UnsetDefaultAsync(request.AccountId, ct);
        }

        var entity = new Address(
            request.AccountId,
            request.Label,
            provinceName,
            provinceName,
            districtName,
            wardName,
            request.Street,
            request.Detail,
            request.IsDefault,
            request.ProvinceId,
            request.DistrictId,
            request.WardId.ToString()
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