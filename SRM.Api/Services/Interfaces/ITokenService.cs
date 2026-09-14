using SRM.Api.Models.Enums;
using SRM.Api.Utils;

namespace SRM.Api.Services.Interfaces
{
    public interface ITokenService
    {
        Result<string> CreateToken(Guid id, string email, UserType userType);
    }
}
