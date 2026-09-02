using Microsoft.EntityFrameworkCore;
using SRM.Api.Data;
using SRM.Api.Repositories.Interfaces;
using SRM.Api.Utils;
using SRM.Api.Models.Enums;

namespace SRM.Api.Repositories
{
    public class ReservationRepository(AppDbContext _db) : IReservationRepository
    {
        public async Task<Result<bool>> ReservationDateValid(DateTime CheckInDate, DateTime CheckOutDate)
        {
            var hasConflict = await _db.Reservations.AnyAsync(r =>
                (r.CheckInDate < CheckOutDate &&
                r.CheckOutDate > CheckInDate) &&
                (r.State == ReservationState.ConfirmedPaymentComplete || r.State == ReservationState.ConfirmedPaymentComplete || r.State == ReservationState.PaymentPending)
            );
            return Result<bool>.Ok(hasConflict);
        }
    }
}
