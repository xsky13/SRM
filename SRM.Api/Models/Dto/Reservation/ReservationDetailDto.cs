using SRM.Api.Models.Enums;
using SRM.Api.Models.Entities;
using SRM.Api.Models.Dto.Payment;
using System;
using System.Collections.Generic;
using System.Text;

namespace SRM.Api.Models.Dto.Reservation
{
    public class ReservationDetailDto
    {
        public Guid ResrevationId { get; set; }
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public Guid ApartmentId { get; set; }
        public ReservationState State { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public List< PaymentDto> Payments { get; set; }

    }
}
