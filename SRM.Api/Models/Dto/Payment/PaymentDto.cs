using SRM.Api.Models.Enums;

namespace SRM.Api.Models.Dto.Payment
{
    public record PaymentDto
    (
        Guid Id,
        decimal Amount,
        bool IsManual,
        bool IsSign,
        DateTime PaymentDate,
        PaymentStatus PaymentStatus,
        Guid ReservationId,
        Guid AppUserId,
        Guid? TicketId
    );
}
