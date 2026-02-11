using Modules.User.Domain.Enums;
namespace Modules.User.Application.DTOs.Response
{
    public class ProfileResDto
    {
        public string LastName { get; set; } = "";
        public string? Bio { get; set; } = "Xin chào.";
        public string? PhoneNumber { get; set; }
        public Gender Gender { get; set; } = Gender.Other;
        public string? AvatarUrl { get; set; }
        public DateTime? DateOfBirth { get; set; }
    }
}