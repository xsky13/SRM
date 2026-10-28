using SRM.Api.Models.Dto.User;
using SRM.Api.Models.Entities;
using SRM.Api.Models.Enums;
using SRM.Api.Utils;

namespace SRM.Api.Services.Interfaces
{
    public interface IUserService
    {
        AppUser CreateUser(string firstName, string lastName, string telefono, string email, string pwd, UserType userTyp);
        AppUser CreateUser(string email);
        Task<Result<bool>> ValidateUser(AppUser user);
        Task<bool> UserExists(Guid userId);
        Task<Result<UserListingDto>> GetUser(Guid userId);
        Task<Result<List<UserSearchDto>>> SearchUsersAsync(string? query, int limit);
        Task DeleteUser(Guid userId);
    }
}