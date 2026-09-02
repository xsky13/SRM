using SRM.Api.Utils;

namespace SRM.Api.Repositories.Interfaces
{
    public interface IReservationRepository
    {
        Task<Result<bool>> ReservationDateValid(DateTime CheckInDate, DateTime CheckOutDate);
    }
}
