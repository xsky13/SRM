using SRM.Api.Models.Dto.Payment;

namespace SRM.Api.Models.Dto.Reservation
{
    public class EditOwnReservationRequestDto : MpPaymentObject
    {
        public DateTime NewStartDate { get; set; }
        public DateTime NewEndDate { get; set; }
    }
}
