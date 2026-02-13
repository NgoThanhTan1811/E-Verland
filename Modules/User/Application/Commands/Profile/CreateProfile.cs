using Modules.User.Application.Interfaces.Repositories;
using Modules.User.Application.DTOs.Response;
using Modules.User.Domain.Entities;
using MediatR;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using Modules.User.Application.Validators;

namespace Modules.User.Application.Commands
{
    public sealed record CreateProfileCommand(
        Guid AccountId,
        string FirstName,
        string LastName,
        DateTime DateOfBirth,
        string PhoneNumber,
        string Email
    ) : IRequest<ProfileResDto>;

    public class ProfileHandler : IRequestHandler<CreateProfileCommand, ProfileResDto>
    {
        private readonly IProfileRepository _repo;
        private readonly IUserDbContext _db;
        private readonly IMapper _mapper;

        public ProfileHandler(IProfileRepository repo, IMapper mapper, IUserDbContext db)
        {
            _repo = repo;
            _mapper = mapper;
            _db = db;
        }

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
            return _mapper.Map<ProfileResDto>(entity);
        }
    }
}