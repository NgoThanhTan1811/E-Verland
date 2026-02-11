using Modules.User.Domain.Enums;

namespace Modules.User.Application.DTOs.Response
{
    public class AccountResDto
    {
        public string NormalizedUsername { get; private set; } = default!;
        public string NormalizedEmail { get; private set; } = default!;

    }



}