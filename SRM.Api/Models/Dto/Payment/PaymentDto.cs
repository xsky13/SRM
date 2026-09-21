using SRM.Api.Models.Enums;

namespace SRM.Api.Models.Dto.Payment
{
    public class PaymentDto
    {
        public Guid Id { get; set; }
        public decimal Amount { get; set; }
        public bool IsManual { get; set; }
        public bool IsSign { get; set; }
        public DateTime PaymentDate { get; set; }
        public PaymentStatus PaymentStatus { get; set; }
        public Guid ReservationId { get; set; }
        public Guid AppUserId { get; set; }
        public Guid? TicketId { get; set; }
    }

    public class PaymentWithUserEmailDto : PaymentDto
    {
        public string Email { get; set; }
    }
}
