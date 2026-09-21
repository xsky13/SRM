namespace SRM.Api.Services
{
    public class UserService(AppDbContext _db, ITokenService _tokenService) : IUserService
    {

        public async Task<Result<void>> CreateUser(string firstName, string lastName, string telefono, string email, string pwd, UserType userType)
        {

            var newUser = new AppUser
            {
                Name = firstName,
                LastName = lastName,
                Email = email,
                Telefono = telefono,
                PwdHash = BCrypt.Net.BCrypt.HashPassword(pwd),
                Usertype = userType
            };

            _db.AppUsers.Add(newUser);

        }

        Task<Result<void>> CreateUser(string email)
        {
            var newUser = new AppUser
            {
                Id = Guid.NewGuid(),
                Name = "",
                LastName = "",
                Email = email
                Telefono = "",
                Usertype = UserType.Guest
            };
            _db.AppUsers.Add(newUser);

        }

        Task<Result<bool>> ValidateUser(AppUser user)
        { 

            if (string.IsNullOrWhiteSpace(user.Name))
                return Result<bool>.Fail("El primer nombre es obligatorio.");

            if (string.IsNullOrWhiteSpace(user.LastName))
                return Result<bool>.Fail("El apellido es obligatorio.");

            if (!Regex.IsMatch(user.Telefono, @"^[+]*[(]{0,1}[0-9]{1,4}[)]{0,1}[-\s\./0-9]*$"))
                return Result<bool>.Fail("Telefono invalido.");

            if (string.IsNullOrWhiteSpace(user.Email))
                return Result<bool>.Fail("El email es obligatorio.");

            if (string.IsNullOrWhiteSpace(user.PwdHash) || user.PwdHash.Length < 6)
                return Result<bool>.Fail("La contraseña debe tener al menos 6 caracteres.");

            if (await _db.AppUsers.AnyAsync(u => u.Email == user.Email))
                return Result<bool>.Fail("Ya existe un usuario con ese email.");
            
            return Result<bool>.Ok(true);
        }


        
           

        


}