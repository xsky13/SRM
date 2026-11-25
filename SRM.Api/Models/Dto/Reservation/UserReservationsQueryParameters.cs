using SRM.Api.Models.Enums;
using SRM.Api.Models.Pagination;

namespace SRM.Api.Models.Dto.Reservation
{
    public class UserReservationsQueryParameters : PaginationParameters
    {
        public ReservationState State { get; set; }
    }
}
