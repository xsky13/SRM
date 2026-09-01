using SRM.Api.Data;
using System;
using System.Collections.Generic;
using System.Text;
using SRM.Api.Repositories;
using SRM.Api.Models.Dto.Reservation;
using SRM.Api.Models.Dto.Apartment;
namespace SRM.Api.Repositories.Interfaces;

public class ReservationRepository(AppDbContext _db) : IReservationRespository
{

    public async Task<ReservationListingDto> GetAll(Guid apartmentId)
    {
        var reservations = await _db.Reservations
            .AsNoTracking()
            .Where(async r => r.ApartmentId == apartmentId)
            .Select(r => new ReservationListingDto(
                r.Id,
                r.CheckInDate,
                r.CheckOutDate,
                r.ApartmentId
                ))
               .ToListasync();
        //falta validar que solo reciba informacino de los ultimos dos meses
        return reservations;

    }

    public async Task<ReservationDetailDto> GetById(Guid id)
    {
        var reservation = await _db.Reservations
            .AsNoTracking()
            .Where(async r => r.Id == id)
            .Select(r => new ReservationDetailDto(
                r.Id,
                r.CheckInDate,
                r.CheckOutDate,
                r.ApartmentId
            ))
            .FirstOrDefaultAsync();
        return reservation;

    }




}
