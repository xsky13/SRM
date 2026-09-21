using SRM.Api.Models.Enums;

namespace SRM.Api.Models.Dto.User
{
    public class UserListingDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public string? PwdHash { get; set; }
        public UserType Usertype { get; set; }
    }
}
