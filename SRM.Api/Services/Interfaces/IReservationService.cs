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
        public Task<Result<ReservationDetailDto>> GetReservationById(Guid id);
        public Task<Result<List<ReservationListingDto>>> GetByUserId(Guid userId);

        public Task<bool> DatesAreInvalid(DateTime checkOutDate, DateTime checkInDate, Guid apartmentId);
        public Task<Result<Reservation>> CreateReservation(DateTime checkInDate, DateTime checkOutDate, Guid apartmentId, Guid userId);
    }
}
