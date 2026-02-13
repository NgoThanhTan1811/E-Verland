using Modules.User.Domain.Enums;

namespace Modules.User.Application.DTOs.Response
{
    public class ProfileResDto
    {
        public Guid Id { get; set; }
        public Guid AccountId { get; set; }
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string? Bio { get; set; }
        public string? PhoneNumber { get; set; }
        public Gender Gender { get; set; }
        public string? AvatarUrl { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}