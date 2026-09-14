using SRM.Api.Models.Dto.User;
using SRM.Api.Models.Enums;

namespace SRM.Api.Models.Entities
{
    public class AppUser : ISoftDeletable 
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string  LastName { get; set; } = string.Empty;
        public string Email  { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public string? PwdHash { get; set; }
        public UserType Usertype { get; set; }

        public bool IsDeleted { get; set; }
        public DateTime? DeletedOnUTC { get; set; }

        public List<Reservation> Reservations { get; set; } = [];
        public List<Payment> Payments { get; set; } = [];

        public UserListingDto ToListingDto()
        {
            return new UserListingDto
            {
                Id = Id,
                Name = Name,
                LastName = LastName,
                Telefono = Telefono,
                Email = Email,
                PwdHash = PwdHash,
            };
        }
    }
}
