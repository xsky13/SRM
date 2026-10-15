namespace SRM.Api.Models.Dto.Reservation
{
    public class CreateReservationRequestDto
    {
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public Guid ApartmentId { get; set; }
    }
}
