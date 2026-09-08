using System;
using System.Collections.Generic;
using System.Text;

namespace SRM.Api.Models.Dto.Reservation
{
    internal class ReservationDetailDto
    {
        Guid ResrevationId;
        DateTime CheckInDate;
        DateTime CheckOutDate;
        Guid ApartmenId;

    }
}
