using SRM.Api.Models.Dto.User;
using SRM.Api.Models.Entities;
using SRM.Api.Models.Enums;
using SRM.Api.Models.Pagination;
using SRM.Api.Utils;

namespace SRM.Api.Services.Interfaces
{
    public interface IUserService
    {
        AppUser CreateUser(string firstName, string lastName, string telefono, string email, string pwd, UserType userTyp);
        Task<Result<UserListingDto>> GetUserById(Guid userId);
        AppUser CreateUser(string email);
        Task<Result<bool>> ValidateUser(AppUser user, Guid? excludeUserId = null);
        Task<Result<UserListingDto>> UpdateUser(Guid userId, UpdateUserRequestDto request);
        Task<bool> UserExists(Guid userId);
        Task<Result<UserListingDto>> GetUser(Guid userId);
        Task<List<UserListingDto>> ListUsers(PaginationParameters filter);
        Task<Result<List<UserSearchDto>>> SearchUsersAsync(string? query, PaginationParameters filter);
        Task DeleteUser(Guid userId);
    }
}