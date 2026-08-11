using SRM.Api.Models.Enums;

namespace SRM.Api.Models.Entities
{
    public class Reservation
    {
        public Guid Id { get; set; }
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate  { get; set; }
        public ReservationState State { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public bool IsDeleted { get; set; }
        public DateTime? DeletedOnUTC { get; set; }

        public int ApartamentId { get; set; }
        public Apartment Apartment { get; set; } = null!;

        public int AppUserId { get; set; }
        public AppUser AppUser { get; set; } = null!;
    }
}
