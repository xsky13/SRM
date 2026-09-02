using SRM.Api.Data;
using SRM.Api.Models.Dto.Apartment;
using SRM.Api.Models.Dto.Reservation;
using SRM.Api.Models.Entities;
using SRM.Api.Repositories.Interfaces;
using SRM.Api.Repositories.Interfaces;
using SRM.Api.Services.Interfaces;
using SRM.Api.Utils;
using System;
using System.Collections.Generic;
using System.Text;



namespace SRM.Api.Services
{
    internal class ReservationService(AppDbContext _db) : I
    {
        public async Task<Result<ReservationDetailDto>> GetByReservationId(Guid reservationId) {



            var reservation = await _db.Reservations
                .AsNoTracking()
                .Where(r => r.Id == reservationId)
                .Select(r => new ReservationDetailDto(
                    r.Id,
                    r.CheckInDate,
                    r.CheckOutDate,
                    r.ApartmentId
                ))
                .FirstOrDefaultAsync();

            return Result<List<ReservationDetailDto>>.Ok(reservation);
            
        }

        public async Task<Result<ReservationListingDto>> GetByApartmentId(Guid id)
        {
            var reservations = await _db.Reservations
            .AsNoTracking()
            .Where(r => r.ApartmentId == apartmentId)
            .Select(r => new ReservationListingDto(
                r.Id,
                r.CheckInDate,
                r.CheckOutDate,
                r.ApartmentId
            ))
            .ToListAsync();

            return Result<List<ReservationListingDto>>.Ok(reservations);

        }
    }
}
