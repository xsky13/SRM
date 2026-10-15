using Microsoft.EntityFrameworkCore;
using SRM.Api.Data;
using SRM.Api.Models.Dto.User;
using SRM.Api.Models.Entities;
using SRM.Api.Models.Enums;
using SRM.Api.Services.Interfaces;
using SRM.Api.Utils;
using System.Text.RegularExpressions;

namespace SRM.Api.Services
{
    public class UserService(AppDbContext _db) : IUserService
    {

        public AppUser CreateUser(string firstName, string lastName, string telefono, string email, string pwd, UserType userType)
        {

            var newUser = new AppUser
            {
                Id = Guid.NewGuid(),
                Name = firstName,
                LastName = lastName,
                Email = email,
                Telefono = telefono,
                PwdHash = BCrypt.Net.BCrypt.HashPassword(pwd),
                Usertype = userType
            };

            _db.AppUsers.Add(newUser);
            return newUser;
        }

        public AppUser CreateUser(string email)
        {
            var newUser = new AppUser
            {
                Id = Guid.NewGuid(),
                Name = "",
                LastName = "",
                Email = email,
                Telefono = "",
                Usertype = UserType.Guest
            };
            _db.AppUsers.Add(newUser);
            return newUser;
        }

        public async Task<Result<bool>> ValidateUser(AppUser user)
        {
            if (string.IsNullOrWhiteSpace(user.Name))
                return Result<bool>.Fail("El primer nombre es obligatorio.");

            if (string.IsNullOrWhiteSpace(user.LastName))
                return Result<bool>.Fail("El apellido es obligatorio.");

            if (!Regex.IsMatch(user.Telefono, @"^[+]*[(]{0,1}[0-9]{1,4}[)]{0,1}[-\s\./0-9]*$"))
                return Result<bool>.Fail("Telefono invalido.");

            if (string.IsNullOrWhiteSpace(user.Email))
                return Result<bool>.Fail("El email es obligatorio.");

            if (await _db.AppUsers.AnyAsync(u => u.Email == user.Email))
                return Result<bool>.Fail("Ya existe un usuario con ese email.");

            return Result<bool>.Ok(true);
        }

        public async Task<bool> UserExists(Guid userId)
        {
            return await _db.AppUsers.AnyAsync(u => u.Id == userId);
        }

        public async Task<Result<UserListingDto>> GetUser(Guid userId)
        {
            var user = await _db.AppUsers.FindAsync(userId);
            if (user == null) return Result<UserListingDto>.Fail("No existe el usuario");

            return Result<UserListingDto>.Ok(user.ToListingDto());
        }
    }
}