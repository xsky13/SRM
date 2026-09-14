using MercadoPago.Resource.User;
using Microsoft.EntityFrameworkCore;
using SRM.Api.Data;
using SRM.Api.Models.Entities;
using SRM.Api.Services.Interfaces;
using SRM.Api.Utils;
using System.Text.RegularExpressions;

namespace SRM.Api.Services
{
    public class AuthService(AppDbContext _db, ITokenService _tokenService) : IAuthService
    {
        public async Task<Result<string>> LoginUser(string email, string pwd)
        {
            // find user with email
            var userWithEmail = await _db.AppUsers.FirstOrDefaultAsync(user => user.Email == email);

            if (userWithEmail == null)
                return Result<string>.Fail("El usuario con ese email no existe.");

            // compare hash
            if (!BCrypt.Net.BCrypt.Verify(pwd, userWithEmail.PwdHash))
                return Result<string>.Fail("Contrasena incorrecta.");

            // return token
            var token = _tokenService.CreateToken(userWithEmail.Id, userWithEmail.Email, userWithEmail.Usertype);
            return Result<string>.Ok(token.Value!);
        }

        public async Task<Result<string>> RegisterUser(string firstName, string lastName, string telefono, string email, string pwd)
        {
            if (string.IsNullOrWhiteSpace(firstName))
                return Result<string>.Fail("El primer nombre es obligatorio.");

            if (string.IsNullOrWhiteSpace(lastName))
                return Result<string>.Fail("El apellido es obligatorio.");

            if (!Regex.IsMatch(telefono, @"^[+]*[(]{0,1}[0-9]{1,4}[)]{0,1}[-\s\./0-9]*$"))
                return Result<string>.Fail("Telefono invalido.");

            if (string.IsNullOrWhiteSpace(email))
                return Result<string>.Fail("El email es obligatorio.");

            if (string.IsNullOrWhiteSpace(pwd) || pwd.Length < 6)
                return Result<string>.Fail("La contraseña debe tener al menos 6 caracteres.");

            if (await _db.AppUsers.AnyAsync(user => user.Email == email))
                return Result<string>.Fail("Ya existe un usuario con ese email.");

            var newUser = new AppUser
            {
                Name = firstName,
                LastName = lastName,
                Email = email,
                PwdHash = BCrypt.Net.BCrypt.HashPassword(pwd),
                Usertype = Models.Enums.UserType.User
            };

            _db.AppUsers.Add(newUser);
            await _db.SaveChangesAsync();

            var token = _tokenService.CreateToken(newUser.Id, newUser.Email, newUser.Usertype);
            return Result<string>.Ok(token.Value!);
        }
    }
}
