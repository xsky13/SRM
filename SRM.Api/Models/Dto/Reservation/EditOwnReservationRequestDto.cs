using SRM.Api.Models.Dto.Payment;

namespace SRM.Api.Models.Dto.Reservation
{
    public class EditOwnReservationRequestNoPaymentDto
    {
        public DateTime NewStartDate { get; set; }
        public DateTime NewEndDate { get; set; }
    }
    public class EditOwnReservationRequestPaymentDto : MpPaymentObject
    {
        public DateTime NewStartDate { get; set; }
        public DateTime NewEndDate { get; set; }
    }
}
