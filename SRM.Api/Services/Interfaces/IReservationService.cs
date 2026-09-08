using SRM.Api.Models.Dto.Apartment;
using SRM.Api.Models.Dto.Reservation;
using SRM.Api.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace SRM.Api.Services.Interfaces
{
    internal interface IReservationService
    {
        Task<Result<List<ReservationListingDto>>> GetAll(Guid apartmentId);

        Task<Result<ReservationDetailDto>> GetById(Guid id);
    }
}
