using SRM.Api.Models.Dto.Apartment;
using System;
using System.Collections.Generic;
using System.Text;
using SRM.Api.Models.Dto.Reservation;


namespace SRM.Api.Repositories.Interfaces
{
    internal interface IReservationRepository
    {
        Task<List<ReservationListingDto>> GetAll();
        Task<ReservationDetailsDto?> GetById(Guid id);
    }
}
