using Modules.User.Domain.Enums;

namespace Modules.User.Application.DTOs.Response
{
    public class AccountResDto
    {
        public Guid Id { get; set; }
        public string NormalizedUsername { get; private set; } = default!;
        public string NormalizedEmail { get; private set; } = default!;

    }



}