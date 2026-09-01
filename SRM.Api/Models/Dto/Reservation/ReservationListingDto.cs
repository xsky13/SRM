using System;
using System.Collections.Generic;
using System.Text;

namespace SRM.Api.Models.Dto.Reservation
{
    internal class ReservationListingDto
    {
        public Guid Id { get; set; }
        public DateTime CheckInDate { get; set; } //
        public DateTime CheckOutDate { get; set; } //
        public Guid ApartmentId { get; set; } //




    }
}
