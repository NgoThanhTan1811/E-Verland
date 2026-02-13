using Modules.User.Domain.Enums;

namespace Modules.User.Application.DTOs.Response
{
    public class AccountResDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = default!;
        public string Username { get; set; } = default!;
        public string NormalizedUsername { get; set; } = default!;
        public string NormalizedEmail { get; set; } = default!;
        public RoleUser Role { get; set; }
        public StatusUser Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}