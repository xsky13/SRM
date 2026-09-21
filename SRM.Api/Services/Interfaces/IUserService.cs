using SRM.Api.Dtos.Entities.User;
using SRM.Api.Models.Enums;

namespace SRM.Api.Services.Interfaces
{
    public interface IUserService
    {
        Task<Result<void>> CreateUser(string firstName, string lastName, string telefono, string email, string pwd, UserType userType);
        Task<Result<void>> CreateUser(Guid id, string firstName, string lastName, string telefono, string email, string pwd, UserType userType);

    }
}