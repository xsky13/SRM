using Microsoft.EntityFrameworkCore;
using PhoneNumbers;
using SRM.Api.Data;
using SRM.Api.Models.Dto.User;
using SRM.Api.Models.Entities;
using SRM.Api.Models.Enums;
using SRM.Api.Models.Pagination;
using SRM.Api.Services.Interfaces;
using SRM.Api.Utils;
using SRM.Api.Utils.Extensions;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.RegularExpressions;

namespace SRM.Api.Services
{
    public class UserService(AppDbContext _db) : IUserService
    {
        private static readonly PhoneNumberUtil PhoneUtil = PhoneNumberUtil.GetInstance();
        private static readonly Regex NameRegex = new(
            @"^\p{L}[\p{L}\p{M}]*(?:[ '\u2019-]\p{L}[\p{L}\p{M}]*)*\z",
            RegexOptions.CultureInvariant,
            TimeSpan.FromMilliseconds(100));

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


        public string? NormalizePhone(string? phone)
        {
            if (string.IsNullOrWhiteSpace(phone)) return null;
            try
            {
                var number = PhoneUtil.Parse(phone, "AR");
                return PhoneUtil.IsValidNumber(number) ? PhoneUtil.Format(number, PhoneNumberFormat.E164) : null;
            }
            catch (NumberParseException) { return null; }
        }

        private static string NormalizeName(string s) =>
            Regex.Replace(s.Trim(), @"\s+", " ").Normalize(NormalizationForm.FormC);

        private static Result<bool>? ValidateName(string? raw, string label)
        {
            if (string.IsNullOrWhiteSpace(raw)) return Result<bool>.Fail($"{label} es obligatorio.");
            var name = NormalizeName(raw);
            if (name.Length > 100) return Result<bool>.Fail($"{label} es demasiado largo.");
            if (!NameRegex.IsMatch(name)) return Result<bool>.Fail($"{label} contiene caracteres invalidos.");
            return null;
        }

        public async Task<Result<bool>> ValidateUser(AppUser user, Guid? excludeUserId = null)
        {
            if (user is null)
                return Result<bool>.Fail("Usuario invalido.");

            var nameValidation = ValidateName(user.Name, "El nombre");
            if (nameValidation != null)
                return nameValidation;

            var lastNameValidation = ValidateName(user.LastName, "El apellido");
            if (lastNameValidation != null)
                return lastNameValidation;

            user.Name = NormalizeName(user.Name!);
            user.LastName = NormalizeName(user.LastName!);

            user.Email = user.Email?.Trim().ToLowerInvariant();
            //user.Telefono = user.Telefono?.Trim();

            var phone = NormalizePhone(user.Telefono);
            if (phone == null) return Result<bool>.Fail("Telefono invalido.");
            user.Telefono = phone;

            if (string.IsNullOrWhiteSpace(user.Email))
                return Result<bool>.Fail("El email es obligatorio.");

            if (!new EmailAddressAttribute().IsValid(user.Email))
                return Result<bool>.Fail("El email es invalido.");

            //if (await _db.AppUsers.AnyAsync(u => u.Email == user.Email))
            //    return Result<bool>.Fail("Ya existe un usuario con ese email.");

            if (await _db.AppUsers.AnyAsync(u =>
                u.Email == user.Email &&
                u.Id != excludeUserId &&
                u.Usertype != UserType.Guest))
            {
                return Result<bool>.Fail("Ya existe un usuario con ese email.");
            }
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
        
        public async Task<Result<List<UserSearchDto>>> SearchUsersAsync(string? query, PaginationParameters filter)
        {
            if (filter.PageNumber < 1 || filter.PageNumber > 100_000)
                return Result<List<UserSearchDto>>.Fail("La pagina debe ser mayor que 0.");

            if (filter.PageSize < 1 || filter.PageSize > 50)
                return Result<List<UserSearchDto>>.Fail($"El tamaño de pagina debe estar entre 1 y {50}.");

            var usersQuery = _db.AppUsers
                .AsNoTracking()
                .AsQueryable();

            var term = query?.Trim();
            if (!string.IsNullOrEmpty(term))
            {
                if (term.Length > 100)
                    return Result<List<UserSearchDto>>.Fail("La busqueda es demasiado larga.");

                var pattern = $"%{Escape.EscapeLike(term)}%";
                usersQuery = usersQuery.Where(u =>
                    EF.Functions.ILike(u.Name, pattern) ||
                    EF.Functions.ILike(u.LastName, pattern) ||
                    EF.Functions.ILike(u.Email, pattern));
            }

            var response = await usersQuery
                .OrderBy(u => u.LastName).ThenBy(u => u.Name).ThenBy(u => u.Id)
                .Select(u => new UserSearchDto
                {
                    Id = u.Id,
                    Name = u.Name,
                    LastName = u.LastName,
                    Email = u.Email,
                    Telefono = u.Telefono,
                    Usertype = u.Usertype
                })
                .ToPagedResponseAsync(filter.PageNumber, filter.PageSize);
            var users = response.Data.ToList();

            return Result<List<UserSearchDto>>.Ok(users);
        }

        public async Task<Result<UserListingDto>> UpdateUser(Guid userId, UpdateUserRequestDto request)
        {
            var user = await _db.AppUsers.FindAsync(userId);

            if (user == null)
                return Result<UserListingDto>.Fail("El usuario no existe.");

            user.Name = request.FirstName;
            user.LastName = request.LastName;
            user.Email = request.Email;
            user.Telefono = request.Telefono;

            var validation = await ValidateUser(user, user.Id);

            if (!validation.Success)
                return Result<UserListingDto>.Fail(validation.Error!);

            await _db.SaveChangesAsync();

            return Result<UserListingDto>.Ok(user.ToListingDto());
        }

        public async Task<List<UserListingDto>> ListUsers(PaginationParameters filter)
        {
            var query = _db.AppUsers
                .AsNoTracking()
                .OrderBy(u => u.Id);

            var response = await query.ToPagedResponseAsync(filter.PageNumber, filter.PageSize);
            var users = response.Data.Select(u => u.ToListingDto()).ToList();

            return users;
        }

        public async Task<Result<UserListingDto>> GetUserById(Guid userId)
        {
            var user = await _db.AppUsers
                .Where(u => u.Id == userId)
                .Select(u => u.ToListingDto())
                .FirstOrDefaultAsync();

            if (user == null) return Result<UserListingDto>.Fail("El usuario no existe", 404);

            return Result<UserListingDto>.Ok(user);
        }

        public async Task<Result<bool>> UserCanDeleteAccount(Guid userId)
        {
            if (!await UserExists(userId)) return Result<bool>.Fail("El usuario no existe.");

            var userReservations = await _db.Reservations
                .Where(r => r.CheckOutDate.Date >= DateTime.UtcNow.Date && r.State != ReservationState.Cancelled && r.AppUserId == userId)
                .AnyAsync();

            if (userReservations) return Result<bool>.Fail("No puede eliminar su cuenta con reservas pendientes. Si realmente quiere continuar, debe cancelar todas las que tiene programadas a futuro.");

            return Result<bool>.Ok(true);

        }
    }
}