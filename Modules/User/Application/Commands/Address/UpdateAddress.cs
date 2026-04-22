using Modules.User.Application.Interfaces.Repositories;
using Modules.User.Application.DTOs.Response;
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
        string? City,
        string? Province,
        string? District,
        string? Ward,
        string? Street,
        string? Detail,
        bool? IsDefault
    ) : IRequest<AddressResDto>;

    public sealed class UpdateAddressHandler(IAddressRepository repo, IUserDbContext db)
                : IRequestHandler<UpdateAddressCommand, AddressResDto>
    {
        private readonly IAddressRepository _repo = repo;
        private readonly IUserDbContext _db = db;

        public async Task<AddressResDto> Handle(UpdateAddressCommand request, CancellationToken ct)
        {
            var entity = await _repo.GetByIdForProfileAsync(request.AddressId, request.ProfileId, ct)
                ?? throw new KeyNotFoundException("Address not found.");

            var validationResult = AddressValidator.UpdateAddress.Validate(new DTOs.Request.UpdateAddressReqDto
            {
                Label = request.LableAddress,
                City = request.City,
                Province = request.Province,
                District = request.District,
                Ward = request.Ward,
                Street = request.Street,
                Detail = request.Detail,
                IsDefault = request.IsDefault
            });

            if (validationResult != ValidationResult.Success)
                throw new ValidationException(validationResult?.ErrorMessage);


            if (request.IsDefault == true && !entity.IsDefault)
            {
                await _repo.UnsetDefaultAsync(request.ProfileId, ct);
            }

            entity.Update(
                request.LableAddress,
                request.City,
                request.Province,
                request.District,
                request.Ward,
                request.Street,
                request.Detail,
                request.IsDefault
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