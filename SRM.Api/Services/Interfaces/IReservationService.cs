using SRM.Api.Models.Dto.Apartment;
using SRM.Api.Models.Dto.Reservation;
using SRM.Api.Models.Entities;
using SRM.Api.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace SRM.Api.Services.Interfaces
{
    public interface IReservationService
    {
        public Task<Result<List<ReservationListingDto>>> GetAllByApartmentId(Guid apartmentId);
        public Task<Result<ReservationDetailDto>> GetReservationById(Guid id, Guid userId);
        public Task<Result<List<ReservationListingDto>>> GetByUserId(Guid userId);
        public Task<Result<List<ReservationListingDto>>> GetAllForAdmin();

        public Task<bool> DatesAreInvalid(DateTime checkOutDate, DateTime checkInDate, Guid apartmentId, Guid? excludeReservationId = null);
        public Task<Result<Reservation>> CreateReservation(DateTime checkInDate, DateTime checkOutDate, Guid apartmentId, Guid userId);
        public Task<Result<Reservation>> CreateAuthReservation(DateTime checkInDate, DateTime checkOutDate, Guid apartmentId, Guid userId);
        public Task<Result<Reservation>> AdminCreateReservationAsync(AdminCreateReservationDto request);
        public Task SaveChanges();

        Task<Result<Reservation>> ValidateReservation(DateTime newEndDate, DateTime newStartDate, Guid reservationId, Guid userId);

        Task<Result<ReservationDetailDto>> EditOwnReservation(EditOwnReservationRequestNoPaymentDto request, Reservation reservation, Guid userId);
        Task<Result<ReservationDetailDto>> EditOwnReservationFull(EditOwnReservationRequestPaymentDto request, Reservation reservation, Guid userId);

        Task<Result<ReservationDetailDto>> CancelReservation(Guid reservationId, Guid userId);
        Task<Result<ReservationDetailDto>> AdminCancelReservationAsync(Guid reservationId);
    }
}
