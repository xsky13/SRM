using SRM.Api.Models.Entities;
using SRM.Api.Models.Enums;
using SRM.Api.Utils;

namespace SRM.Api.Services.Interfaces
{
    public interface IUserService
    {
        public AppUser CreateUser(string firstName, string lastName, string telefono, string email, string pwd, UserType userTyp);
        public AppUser CreateUser(string email);
        public Task<Result<bool>> ValidateUser(AppUser user);
        public Task<bool> UserExists(Guid userId);

    }
}