using SRM.Api.Models.Dto.Apartment;
using SRM.Api.Models.Dto.Reservation;
using SRM.Api.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using SRM.Api.Utils;
using SRM.Api.Models.Entities;
using System.Security.Claims;

namespace SRM.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReservationController(IReservationService _reservationService, IUserService _userService) : ControllerBase
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

        [HttpGet("user/{userId}")]
        public async Task<ActionResult<List<ReservationListingDto>>> GetByUserId(Guid userId)
        {
            var response = await _reservationService.GetByUserId(userId);
            return response.ToActionResult();
        }

        [HttpPost]
        public async Task<ActionResult<Reservation>> CreateReservation([FromBody] CreateReservationRequestDto request)
        {

            // encontrar el usuario a travez del token de autenticacion
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (userId == null)
                return Unauthorized();

            var userGuid = Guid.Empty;
            if (!Guid.TryParse(userId, out userGuid))
                return BadRequest("Id invalido.");

            // llamar al servicio de usuario para validad al usuario y obtener su guid
            if (!await _userService.UserExists(userGuid))
                return BadRequest("Usuario no encontrado.");

            var response = await _reservationService.CreateReservation(request.CheckInDate, request.CheckOutDate, request.ApartmentId, userGuid);
            await _reservationService.SaveChanges();

            return response.ToActionResult();
        
        }
    }
}
