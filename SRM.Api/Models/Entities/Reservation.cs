using SRM.Api.Models.Dto.Reservation;
using SRM.Api.Models.Enums;

namespace SRM.Api.Models.Entities
{
    public class Reservation
    {
        public Guid Id { get; set; } //
        public DateTime CheckInDate { get; set; } //
        public DateTime CheckOutDate  { get; set; } //
        public ReservationState State { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public bool IsDeleted { get; set; }
        public DateTime? DeletedOnUTC { get; set; }

        public Guid ApartmentId { get; set; } //
        public Apartment Apartment { get; set; } = null!;

        public Guid AppUserId { get; set; }
        public AppUser AppUser { get; set; } = null!;

        public List<Payment> Payments { get; set; }

        public ReservationDetailDto ToReservationDetailDto()
        {
            return new ReservationDetailDto
            {
                ResrevationId = Id,
                CheckInDate = CheckInDate,
                CheckOutDate = CheckOutDate,
                ApartmentId = ApartmentId,
                State = State,
                UpdatedAt = UpdatedAt,
                CreatedAt = CreatedAt,
                Payments = Payments.Select(p => p.ToDto()).ToList()
            };
        }
    }
}
