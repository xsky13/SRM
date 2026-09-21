using SRM.Api.Models.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace SRM.Api.Models.Dto.Reservation
{
    public class ReservationListingDto
    {
        public Guid Id { get; set; }
        public DateTime CheckInDate { get; set; } //
        public DateTime CheckOutDate { get; set; } //
        public Guid ApartmentId { get; set; } //
        public ReservationState ReservationState { get; set; }
    }
}
