namespace SRM.Api.Models.Entities
{
    public class Payment
    {
        public Guid Id { get; set; }
        public float Amount { get; set; }
        public bool IsManual { get; set; }
        public bool IsSign { get; set; }
        public DateTime PaymentDate { get; set; }

        public int ReservationId { get; set; }
        public Reservation Reservation { get; set; } = null!;

        public int AppUserId { get; set; }
        public AppUser AppUser { get; set; } = null!;

        public int? TicketId { get; set; }
        public Ticket? Ticket { get; set; }
    }
}
