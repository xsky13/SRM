using SRM.Api.Models.Dto.Apartment;
using SRM.Api.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using SRM.Api.Models.Dto.Reservation;


namespace SRM.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReservationController(IReservationService _reservationService) : ControllerBase
    {
        [HttpGet("{id}")]
        public async Task<ActionResult<List<ReservationListingDto>>> GetByReservationId()
        {
            var response = await _apartmentService.GetByReservationId();
            return response.ToActionResult();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ReservationDetailDto>> GetByApartmentId(Guid id)
        {
            var response = await _apartmentService.GetByApartmentId(id);
            return response.ToActionResult();
        }
    }
}
