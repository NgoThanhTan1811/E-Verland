
using Modules.User.Application.Interfaces.Repositories;
using Modules.User.Application.DTOs.Response;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Modules.User.Domain.Enums;
using System.ComponentModel.DataAnnotations;
using Modules.User.Application.Validators;
using Modules.User.Application.Mappings;

namespace Modules.User.Application.Commands
{
    public sealed record UpdateProfileCommand(
     Guid AccountId,
    string? FirstName,
    string? LastName,
    string? PhoneNumber,
    DateTime? DateOfBirth,
    string? AvatarUrl,
    Gender? Gender,
    string? Bio

    ) : IRequest<ProfileResDto>;

    public sealed class UpdateProfileHandler(IProfileRepository repo, IUserDbContext db)
                : IRequestHandler<UpdateProfileCommand, ProfileResDto>
    {
        private readonly IProfileRepository _repo = repo;
        private readonly IUserDbContext _db = db;

        public async Task<ProfileResDto> Handle(UpdateProfileCommand request, CancellationToken ct)
        {
            var entity = await _repo.GetByAccountIdAsync(request.AccountId, ct)
                ?? throw new KeyNotFoundException("Profile not found.");

            var validationResult = ProfileValidator.UpdateProfile.Validate(new DTOs.Request.UpdateProfileReqDto
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                DateOfBirth = request.DateOfBirth,
                AvatarUrl = request.AvatarUrl,
                PhoneNumber = request.PhoneNumber,
                Bio = request.Bio,
                Gender = request.Gender
            });

            if (validationResult != ValidationResult.Success)
                throw new ValidationException(validationResult?.ErrorMessage);
            if (request.PhoneNumber is not null)
                entity.PhoneNumber = request.PhoneNumber.Trim();

            // Ensure avatar is stored as a relative path. If client passed a full URL (presigned), extract the key.
            string? avatarRelative = request.AvatarUrl;
            if (!string.IsNullOrWhiteSpace(avatarRelative) && Uri.IsWellFormedUriString(avatarRelative, UriKind.Absolute))
            {
                var u = new Uri(avatarRelative);
                avatarRelative = u.AbsolutePath.TrimStart('/');
            }

            entity.Update(
                request.FirstName,
                request.LastName,
                request.DateOfBirth.HasValue ? DateOnly.FromDateTime(request.DateOfBirth.Value) : (DateOnly?)null,
                avatarRelative,
                request.Gender,
                request.Bio
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