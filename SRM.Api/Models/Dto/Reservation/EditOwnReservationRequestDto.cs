using SRM.Api.Models.Dto.Payment;
using SRM.Api.Utils;
using System.Text.Json.Serialization;

namespace SRM.Api.Models.Dto.Reservation
{
    public class EditOwnReservationRequestNoPaymentDto
    {
        [JsonConverter(typeof(UtcDateJsonConverter))]
        public DateTime NewStartDate { get; set; }

        [JsonConverter(typeof(UtcDateJsonConverter))]
        public DateTime NewEndDate { get; set; }
    }
    public class EditOwnReservationRequestPaymentDto : MpPaymentObject
    {
        [JsonConverter(typeof(UtcDateJsonConverter))]
        public DateTime NewStartDate { get; set; }

        [JsonConverter(typeof(UtcDateJsonConverter))]
        public DateTime NewEndDate { get; set; }
    }
}
