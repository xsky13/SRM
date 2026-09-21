using SRM.Api.Models.Dto.Payment;
using SRM.Api.Models.Enums;

namespace SRM.Api.Models.Entities
{
    public class Payment
    {
        public Guid Id { get; set; }
        public decimal Amount { get; set; }
        public bool IsManual { get; set; }
        public bool IsSign { get; set; }
        public DateTime PaymentDate { get; set; }
        public PaymentStatus PaymentStatus { get; set; }
        public string MpPaymentId { get; set; } = string.Empty;

        public Guid ReservationId { get; set; }
        public Reservation Reservation { get; set; } = null!;

        public Guid AppUserId { get; set; }
        public AppUser AppUser { get; set; } = null!;

        public Guid? TicketId { get; set; }
        public Ticket? Ticket { get; set; }

        public PaymentDto ToDto()
        {
            return new PaymentDto
            {
                Id = Id,
                Amount = Amount,
                IsManual = IsManual,
                IsSign = IsSign,
                PaymentDate = PaymentDate,
                PaymentStatus = PaymentStatus,
                ReservationId = ReservationId,
                AppUserId = AppUserId,
                TicketId = TicketId
            };
        }

        public PaymentWithUserEmailDto ToDtoWithUserEmail()
        {
            return new PaymentWithUserEmailDto
            {
                Id = Id,
                Amount = Amount,
                IsManual = IsManual,
                IsSign = IsSign,
                PaymentDate = PaymentDate,
                PaymentStatus = PaymentStatus,
                ReservationId = ReservationId,
                AppUserId = AppUserId,
                TicketId = TicketId,
                Email = AppUser.Email
            };
        }
    }
}
