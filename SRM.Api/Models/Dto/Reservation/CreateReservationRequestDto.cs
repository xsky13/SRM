using SRM.Api.Utils;
using System.Text.Json.Serialization;

namespace SRM.Api.Models.Dto.Reservation
{
    public class CreateReservationRequestDto
    {
        [JsonConverter(typeof(UtcDateJsonConverter))]
        public DateTime CheckInDate { get; set; }

        [JsonConverter(typeof(UtcDateJsonConverter))]
        public DateTime CheckOutDate { get; set; }
        public Guid ApartmentId { get; set; }
    }
}
