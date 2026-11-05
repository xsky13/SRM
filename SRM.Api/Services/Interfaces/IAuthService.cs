using SRM.Api.Utils;

namespace SRM.Api.Services.Interfaces
{
    public interface IAuthService
    {
        Task<Result<string>> LoginUser(string email, string pwd);
        Task<Result<string>> RegisterUser(string firstName, string lastName, string telefono, string email, string pwd);
    }
}
