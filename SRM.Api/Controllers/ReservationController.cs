using SRM.Api.Models.Dto.Apartment;
using SRM.Api.Models.Dto.Reservation;
using SRM.Api.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SRM.Api.Utils;
using SRM.Api.Models.Entities;
using SRM.Api.Models.Enums;
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
            var userId = User.GetUserId();
            if (userId == null) return Unauthorized();

            var response = await _reservationService.GetReservationById(id, userId.Value);
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

        [Authorize]
        [HttpPost("~/api/booking/admin_create")]
        public async Task<ActionResult<Reservation>> AdminCreateReservation([FromBody] AdminCreateReservationDto request)
        {
            if (!User.IsInRole(UserType.Admin.ToString()))
                return Forbid();

            var response = await _reservationService.AdminCreateReservationAsync(request);
            return response.ToActionResult();
        }

        [HttpPut("{reservationId}")]
        public async Task<ActionResult<ReservationDetailDto>> EditOwnReservation([FromBody] EditOwnReservationRequestNoPaymentDto request, Guid reservationId)
        {
            var userId = User.GetUserId();
            if (userId == null) return Unauthorized();

            if (!await _userService.UserExists(userId.Value))
                return BadRequest("Usuario no encontrado.");

            var reservationValidation = await _reservationService.ValidateReservation(request.NewEndDate, request.NewStartDate, reservationId, userId.Value);
            if (!reservationValidation.Success) return StatusCode(reservationValidation.StatusCode, new { Message = reservationValidation.Error! });
            var reservation = reservationValidation.Value!;

            // cuanto tiempo hay a comparacion de la reserva actual?
            var newNights = Math.Max((request.NewEndDate.Date - request.NewStartDate.Date).Days, 1);
            var oldNights = Math.Max((reservation.CheckOutDate.Date - reservation.CheckInDate.Date).Days, 1);

            // hay menos tiempo
            if (newNights <= oldNights)
            {
                var result = await _reservationService.EditOwnReservation(request, reservation, userId.Value);
                return result.ToActionResult();
            }

            if (reservation.State == Models.Enums.ReservationState.NotConfirmed)
            {
                var result = await _reservationService.EditOwnReservation(request, reservation, userId.Value);
                return result.ToActionResult();
            }

            // no hay ningun otro caso que resolver en este camino
            return BadRequest("Estado de reserva invalido para este caso");
        }

        [HttpPut("{reservationId}/full")]
        public async Task<ActionResult<ReservationDetailDto>> EditOwnReservationFull([FromBody] EditOwnReservationRequestPaymentDto request, Guid reservationId)
        {
            var userId = User.GetUserId();
            if (userId == null) return Unauthorized();

            if (!await _userService.UserExists(userId.Value))
                return BadRequest("Usuario no encontrado.");

            var reservationValidation = await _reservationService.ValidateReservation(request.NewEndDate, request.NewStartDate, reservationId, userId.Value);
            if (!reservationValidation.Success) return StatusCode(reservationValidation.StatusCode, new { Message = reservationValidation.Error! });
            var reservation = reservationValidation.Value!;

            /*
             ESTA FUNCION MANEJA EL CASO DE QUE HAYA PAGOS VALIDOS
            */
            if (reservation.State != Models.Enums.ReservationState.ConfirmedPaymentComplete && reservation.State != Models.Enums.ReservationState.ConfirmedPaymentIncomplete)
                return BadRequest("Esta reserva no esta confirmada.");

            var newNights = Math.Max((request.NewEndDate.Date - request.NewStartDate.Date).Days, 1);
            var oldNights = Math.Max((reservation.CheckOutDate.Date - reservation.CheckInDate.Date).Days, 1);

            // hay menos tiempo (verificar por la dudas)
            if (newNights <= oldNights)
            {
                var editResult = await _reservationService.EditOwnReservation(new EditOwnReservationRequestNoPaymentDto
                {
                    NewEndDate = request.NewEndDate,
                    NewStartDate = request.NewStartDate
                }, reservation, userId.Value);
                return editResult.ToActionResult();
            }

            var result = await _reservationService.EditOwnReservationFull(request, reservation, userId.Value);
            return result.ToActionResult();
        }


        [HttpPost("cancel/{reservationId}")]
        public async Task<ActionResult<ReservationDetailDto>> CancelReservation(Guid reservationId)
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
