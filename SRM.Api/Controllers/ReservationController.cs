using SRM.Api.Models.Dto.Apartment;
using SRM.Api.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using SRM.Api.Models.Dto.Reservation;
using Microsoft.AspNetCore.Mvc;
using SRM.Api.Utils;


namespace SRM.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReservationController(IReservationService _reservationService) : ControllerBase
    {
        [HttpGet("{id}")]
        public async Task<ActionResult<ReservationDetailDto>> GetByReservationId(Guid id)
        {
            var response = await _reservationService.GetReservationById(id);
            return response.ToActionResult();
        }

        [HttpGet("apartment/{id}")]
        public async Task<ActionResult<List<ReservationListingDto>>> GetByApartmentId(Guid id)
        {
            var response = await _reservationService.GetAllByApartmentId(id);
            return response.ToActionResult();
        }
    }
}
