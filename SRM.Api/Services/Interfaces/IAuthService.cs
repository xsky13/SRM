using SRM.Api.Models.Enums;
using SRM.Api.Utils;

namespace SRM.Api.Services.Interfaces
{
    public interface IAuthService
    {
        Result<string> CreateToken(int id, string email, UserType userType);
    }
}
