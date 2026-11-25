using SRM.Api.Models.Enums;
using SRM.Api.Models.Pagination;

namespace SRM.Api.Models.Dto.Reservation
{
    public enum FilterType
    {
        None = 1,
        Past = 2,
        Future = 3
    }
    public class AdminReservationParameterQuery : PaginationParameters
    {
        public FilterType DateFilter { get; set; }
        public ReservationState State { get; set; }
        public bool IncludeDeleted { get; set; }
    }
}
