using Modules.User.Application.Interfaces.Repositories;
using Modules.User.Application.DTOs.Response;
using Modules.User.Application.Interfaces.Services;
using Modules.User.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Modules.User.Domain.Enums;
using System.ComponentModel.DataAnnotations;
using Modules.User.Application.Validators;
using Modules.User.Application.Mappings;

namespace Modules.User.Application.Commands
{
    public sealed record UpdateAddressCommand(
        Guid ProfileId,
        Guid AddressId,
        LableAddress? LableAddress,
        string? Street,
        string? Detail,
        bool? IsDefault,
        int? ProvinceId,
        int? DistrictId,
        int? WardId
    ) : IRequest<AddressResDto>;

    public sealed class UpdateAddressHandler(IAddressRepository repo, IUserDbContext db, ILocationLookupService locations)
                : IRequestHandler<UpdateAddressCommand, AddressResDto>
    {
        private readonly IAddressRepository _repo = repo;
        private readonly IUserDbContext _db = db;
        private readonly ILocationLookupService _locations = locations;

        public async Task<AddressResDto> Handle(UpdateAddressCommand request, CancellationToken ct)
        {
            var entity = await _repo.GetByIdForProfileAsync(request.AddressId, request.ProfileId, ct)
                ?? throw new KeyNotFoundException("Address not found.");

            var validationResult = AddressValidator.UpdateAddress.Validate(new DTOs.Request.UpdateAddressReqDto
            {
                Label = request.LableAddress,
                Street = request.Street,
                Detail = request.Detail,
                IsDefault = request.IsDefault,
                ProvinceId = request.ProvinceId,
                DistrictId = request.DistrictId,
                WardId = request.WardId
            });

            if (validationResult != ValidationResult.Success)
                throw new ValidationException(validationResult?.ErrorMessage);

            var provinceId = request.ProvinceId ?? entity.ProvinceId ?? 0;
            var districtId = request.DistrictId ?? entity.DistrictId ?? 0;
            var wardId = request.WardId ?? (int.TryParse(entity.WardCode, out var parsedWardId) ? parsedWardId : 0);

            if (provinceId <= 0)
                throw new KeyNotFoundException("ProvinceId not found.");

            if (districtId <= 0)
                throw new KeyNotFoundException("DistrictId not found.");

            if (wardId <= 0)
                throw new KeyNotFoundException("WardId not found.");

            var provinceName = await _locations.GetProvinceNameAsync(provinceId, ct)
                ?? throw new KeyNotFoundException("ProvinceId not found.");

            var districtName = await _locations.GetDistrictNameAsync(provinceId, districtId, ct)
                ?? throw new KeyNotFoundException("DistrictId not found.");

            var wardName = await _locations.GetWardNameAsync(provinceId, districtId, wardId, ct)
                ?? throw new KeyNotFoundException("WardId not found.");


            if (request.IsDefault == true && !entity.IsDefault)
            {
                await _repo.UnsetDefaultAsync(request.ProfileId, ct);
            }

            entity.Update(
                request.LableAddress,
                provinceName,
                provinceName,
                districtName,
                wardName,
                request.Street,
                request.Detail,
                request.IsDefault,
                provinceId,
                districtId,
                wardId.ToString()
            );

            try
            {
                await _db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException)
            {
                throw new InvalidOperationException("Update failed due to a database constraint.");
            }

            return entity.ToResDto();
        }
    }
}