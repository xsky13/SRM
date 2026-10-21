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

            return response.ToActionResult();

        }

        [HttpPost("with_auth")]
        public async Task<ActionResult<Reservation>> CreateAuthReservation([FromBody] CreateReservationRequestDto request)
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

            var response = await _reservationService.CreateAuthReservation(request.CheckInDate, request.CheckOutDate, request.ApartmentId, userGuid);

            return response.ToActionResult();

        }

        [HttpPut("{reservationId}")]
        public async Task<ActionResult<ReservationDetailDto>> EditOwnReservation([FromBody] EditOwnReservationRequestNoPaymentDto request, Guid reservationId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (userId == null)
                return Unauthorized();

            if (!Guid.TryParse(userId, out var userGuid))
                return BadRequest("Id invalido.");

            if (!await _userService.UserExists(userGuid))
                return BadRequest("Usuario no encontrado.");

            var reservationValidation = await _reservationService.ValidateReservation(request.NewEndDate, request.NewStartDate, reservationId, userGuid);
            if (!reservationValidation.Success) return BadRequest(reservationValidation.Error!);
            var reservation = reservationValidation.Value!;

            // cuanto tiempo hay a comparacion de la reserva actual?
            var newSpan = request.NewEndDate.Subtract(request.NewStartDate);
            var oldSpan = reservation.CheckOutDate.Subtract(reservation.CheckInDate);

            // hay menos tiempo
            if (newSpan <= oldSpan)
            {
                var result = await _reservationService.EditOwnReservation(request, reservation, userGuid);
                return result.ToActionResult();
            }
            else // hay mas tiempo
            {
                // cual es el estado de la reserva?
                switch (reservation.State)
                {
                    case Models.Enums.ReservationState.NotConfirmed:
                        var result = await _reservationService.EditOwnReservation(request, reservation, userGuid);
                        return result.ToActionResult();
                    case Models.Enums.ReservationState.PaymentPending:
                        return BadRequest("Hay pagos pendientes");
                    default:
                        return BadRequest("Estado de reserva invalido para este caso"); // reservation state was either cancelled, or there is a payment, in which case it will be handled by the other method
                }
            }
        }

        [HttpPut("{reservationId}/full")]
        public async Task<ActionResult<ReservationDetailDto>> EditOwnReservationFull([FromBody] EditOwnReservationRequestPaymentDto request, Guid reservationId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (userId == null)
                return Unauthorized();

            if (!Guid.TryParse(userId, out var userGuid))
                return BadRequest("Id invalido.");

            if (!await _userService.UserExists(userGuid))
                return BadRequest("Usuario no encontrado.");

            var reservationValidation = await _reservationService.ValidateReservation(request.NewEndDate, request.NewStartDate, reservationId, userGuid);
            if (!reservationValidation.Success) return BadRequest(reservationValidation.Error!);
            var reservation = reservationValidation.Value!;

            /*
             ESTA FUNCION MANEJA EL CASO DE QUE HAYA PAGOS VALIDOS
            */
            if (reservation.State != Models.Enums.ReservationState.ConfirmedPaymentComplete && reservation.State != Models.Enums.ReservationState.ConfirmedPaymentIncomplete)
                return BadRequest("Esta reserva no esta confirmada.");

            var result = await _reservationService.EditOwnReservationFull(request, reservation, userGuid);
            return result.ToActionResult();
        }


        [HttpPost("cancel/{reservationId}")]
        public async Task<ActionResult<Reservation>> CancelReservation(Guid reservationId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (userId == null)
                return Unauthorized();
            
            var userGuid = Guid.Empty;
            
            if (!Guid.TryParse(userId, out userGuid))
                return BadRequest("Id invalido.");
            
            if (!await _userService.UserExists(userGuid))
                return BadRequest("Usuario no encontrado.");
            
            var response = await _reservationService.CancelReservation(reservationId, userGuid);
            
            return response.ToActionResult();
        }
    }
}
