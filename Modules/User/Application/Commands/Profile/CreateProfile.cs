using Modules.User.Application.Interfaces.Repositories;
using Modules.User.Application.DTOs.Response;
using Modules.User.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using Modules.User.Application.Validators;
using Modules.User.Application.Mappings;

namespace Modules.User.Application.Commands
{
    public sealed record CreateProfileCommand(
        Guid AccountId,
        string FirstName,
        string LastName,
        DateTime DateOfBirth,
        string PhoneNumber
    ) : IRequest<ProfileResDto>;

    public class ProfileHandler(IProfileRepository repo, IUserDbContext db) : IRequestHandler<CreateProfileCommand, ProfileResDto>
    {
        private readonly IProfileRepository _repo = repo;
        private readonly IUserDbContext _db = db;

        public async Task<ProfileResDto> Handle(CreateProfileCommand request, CancellationToken ct)
        {
            var validationResult = ProfileValidator.CreateProfile.Validate(new DTOs.Request.CreateProfileReqDto
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                PhoneNumber = request.PhoneNumber
            });

            if (validationResult != ValidationResult.Success)
                throw new ValidationException(validationResult?.ErrorMessage);

            var entity = new Domain.Entities.Profile(
                request.AccountId,
                request.FirstName,
                request.LastName,
                request.DateOfBirth
            );

            // Ensure newly created profile has no avatar URL set
            entity.AvatarUrl = null;

            entity.PhoneNumber = request.PhoneNumber?.Trim();

            await _repo.CreateAsync(entity, ct);

            try
            {
                await _db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException)
            {
                throw new InvalidOperationException("Profile creation failed due to database update error.");
            }
            return entity.ToResDto();
        }
    }
}