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
    internal class ReservationService(IReservationRespository reservationRespository) : IReservationService 
    {
        public async Task<Result<ReservationListingDto>> GetAll(Guid apartmentId) {


            ;
            if (apartmentService.GetById(apartmentId) == null)
            {
                return Result<List<ReservationListingDto>>.Fail("No se encontró el departamento", 404);
            }
            else
            {

                var reservations = await reservationRespository.GetAll(apartmentId);
                if (reservations == null)
                {
                    return Result<List<ReservationListingDto>>.Fail("No se encontraron reservaciones", 404);
                }

                return Result<List<ApartmentListingDto>>.Ok(reservations);
            }
        }

        public async Task<Result<ReservationDetailDto>> GetById(Guid id)
        {
            var reservation = await reservationRespository.GetById(id);
            if (reservation == null) 
            {
                return Result<ReservationDetailDto>.Fail("No se encontraron reservaciones", 404);
            }
            return Result<List<ReservationDetailDto>>.Ok(reservation);

        }
    }
}
