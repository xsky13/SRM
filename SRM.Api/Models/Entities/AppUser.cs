using SRM.Api.Models.Enums;

namespace SRM.Api.Models.Entities
{
    public class AppUser : ISoftDeleteable 
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string  LastName { get; set; }
        public string Email  { get; set; }
        public string Telefono { get; set; }
        public string? PwdHash { get; set; }
        public UserType Usertype { get; set; }

        public bool IsDeleted { get; set; }
        public DateTime? DeletedOnUTC { get; set; }

        public List<Reservation> Reservations { get; set; } = [];
        public List<Payment> Payments { get; set; } = [];
    }
}
