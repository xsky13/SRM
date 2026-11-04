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

        public async Task DeleteUser(Guid userId)
        {
            var user = await _db.AppUsers.FindAsync(userId);
            if (user == null) return;
            _db.AppUsers.Remove(user);
            await _db.SaveChangesAsync();
        }


        public async Task<Result<UserListingDto>> GetUser(Guid userId)
        {
            var user = await _db.AppUsers.FindAsync(userId);
            if (user == null) return Result<UserListingDto>.Fail("No existe el usuario");

            return Result<UserListingDto>.Ok(user.ToListingDto());
        }

        public async Task<Result<List<UserSearchDto>>> SearchUsersAsync(string? query, int limit)
        {
            if (limit <= 0)
                return Result<List<UserSearchDto>>.Fail("El límite debe ser mayor que cero.");

            var usersQuery = _db.AppUsers
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(query))
            {
                usersQuery = usersQuery.Where(u =>
                    u.Name.Contains(query) ||
                    u.LastName.Contains(query) ||
                    u.Email.Contains(query));
            }

            var users = await usersQuery
                .Take(limit)
                .Select(u => new UserSearchDto
                {
                    Id = u.Id,
                    FirstName = u.Name,
                    LastName = u.LastName,
                    Email = u.Email,
                    Telefono = u.Telefono
                })
                .ToListAsync();

            return Result<List<UserSearchDto>>.Ok(users);
        }

        public async Task<Result<UserListingDto>> UpdateUser(Guid userId, UpdateUserRequestDto request)
        {
            var user = await _db.AppUsers.FindAsync(userId);
            if (user == null) return Result<UserListingDto>.Fail("El usuario no existe");

            if (string.IsNullOrWhiteSpace(request.FirstName))
                return Result<UserListingDto>.Fail("El nombre es obligatorio.");

            if (request.FirstName.Length > 50)
                return Result<UserListingDto>.Fail("El nombre no puede excederse de 50.");

            if (string.IsNullOrWhiteSpace(request.LastName))
                return Result<UserListingDto>.Fail("El apellido es obligatorio.");

            if (request.LastName.Length > 50)
                return Result<UserListingDto>.Fail("El nombre no puede excederse de 50.");

            if (string.IsNullOrWhiteSpace(request.Telefono))
                return Result<UserListingDto>.Fail("El teléfono es obligatorio.");

            if (request.Telefono.Length > 20)
                return Result<UserListingDto>.Fail("El teléfono no puede superar los 20 caracteres.");

            if (!Regex.IsMatch(request.Telefono, @"^\+?[0-9\s\-()]{7,20}$"))
                return Result<UserListingDto>.Fail("El teléfono no tiene un formato válido.");

            if (string.IsNullOrWhiteSpace(request.Email))
                return Result<UserListingDto>.Fail("El email es obligatorio.");

            if (request.Email.Length > 254)
                return Result<UserListingDto>.Fail("El email no puede superar los 254 caracteres.");

            if (!System.Net.Mail.MailAddress.TryCreate(request.Email, out var emailAddress) ||
                emailAddress.Address != request.Email)
                return Result<UserListingDto>.Fail("El email no tiene un formato válido.");

            var userWithEmail = await _db.AppUsers
                .Where(u => u.Email == request.Email && u.Id != user.Id && u.Usertype != UserType.Guest)
                .AnyAsync();

            if (userWithEmail)
                return Result<UserListingDto>.Fail("Ya hay un usuario con este email.");

            user.Name = request.FirstName;
            user.LastName = request.LastName;
            user.Email = request.Email;
            user.Telefono = request.Telefono;

            await _db.SaveChangesAsync();

            return Result<UserListingDto>.Ok(user.ToListingDto());
        }
    }
}