using SRM.Api.Models.Dto.Reservation;
using SRM.Api.Models.Entities;
using SRM.Api.Models.Pagination;
using SRM.Api.Utils;

namespace SRM.Api.Services.Interfaces
{
    public interface IReservationService
    {
        public Task<Result<List<ReservationListingDto>>> GetAllByApartmentId(Guid apartmentId);
        public Task<Result<ReservationDetailDto>> GetReservationById(Guid id, Guid userId);


        public Task<Result<PagedResponse<ReservationListingDto>>> GetByUserId(Guid userId, PaginationParameters filter);
        public Task<Result<PagedResponse<ReservationListingDto>>> GetByUserIdFuture(Guid userId, UserReservationsQueryParameters filter);
        public Task<Result<PagedResponse<ReservationListingDto>>> GetByUserIdPast(Guid userId, UserReservationsQueryParameters filter);


        public Task<Result<PagedResponse<ReservationDetailDto>>> GetAllForAdmin(AdminReservationParameterQuery filter);

        public Task<bool> DatesAreInvalid(DateTime checkOutDate, DateTime checkInDate, Guid apartmentId, Guid? excludeReservationId = null);
        public Task<Result<Reservation>> CreateReservation(DateTime checkInDate, DateTime checkOutDate, Guid apartmentId, Guid userId);
        public Task<Result<Reservation>> CreateAuthReservation(DateTime checkInDate, DateTime checkOutDate, Guid apartmentId, Guid userId);
        public Task<Result<Reservation>> AdminCreateReservationAsync(AdminCreateReservationDto request);

        public Task<Result<PagedResponse<ReservationDetailDto>>> ListReservationsByApartmentAdmin(Guid apartmentId, AdminReservationParameterQuery filter);
        public Task<Result<PagedResponse<ReservationDetailDto>>> ListReservationsByUserAdmin(Guid userId, AdminReservationParameterQuery filter);


        Task<Result<Reservation>> ValidateReservation(DateTime newEndDate, DateTime newStartDate, Guid reservationId, Guid userId);

        Task<Result<ReservationDetailDto>> EditOwnReservation(EditOwnReservationRequestNoPaymentDto request, Reservation reservation, Guid userId);
        Task<Result<ReservationDetailDto>> EditOwnReservationFull(EditOwnReservationRequestPaymentDto request, Reservation reservation, Guid userId);

        Task<Result<ReservationDetailDto>> CancelReservation(Guid reservationId, Guid userId);
        Task<Result<ReservationDetailDto>> AdminCancelReservationAsync(Guid reservationId);
    }
}
