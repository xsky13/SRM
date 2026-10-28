using SRM.Api.Utils;
using System.Text.Json.Serialization;

namespace SRM.Api.Models.Dto.Reservation
{
    public class AdminCreateReservationDto
    {
        public Guid ApartmentId { get; set; }

        [JsonConverter(typeof(UtcDateJsonConverter))]
        public DateTime StartDate { get; set; }

        [JsonConverter(typeof(UtcDateJsonConverter))]
        public DateTime EndDate { get; set; }

        public bool IsRegisteredUser { get; set; }
        public Guid? UserId { get; set; }
        public GuestUserInfoDto? GuestUserInfo { get; set; }
    }
}
