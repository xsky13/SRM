using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SRM.Api.Data;
using SRM.Api.Models.Dto.Payment;
using SRM.Api.Models.Entities;
using SRM.Api.Services.Interfaces;
using SRM.Api.Utils;
using System.Security.Claims;
using System.Text.Json;

namespace SRM.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentController(IPaymentService paymentService, ILogger<GlobalExceptionHandler> logger, AppDbContext _db) : ControllerBase
    {
        [HttpPost("preference")]
        public async Task<ActionResult<string>> CreatePreference([FromBody] CreatePreferenceRequest request)
        {
            string preferenceId = await paymentService.CreatePreference(request.Title, request.UnitPrice);
            return Ok(preferenceId);
        }

        [HttpPost("process_card_payment/{apartmentId}")]
        public async Task<ActionResult<PaymentWithUserEmailDto>> ProcessPayment([FromBody] CreatePaymentRequest request, Guid apartmentId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            Guid? userId = Guid.TryParse(userIdClaim, out var parsedUserId)
                ? parsedUserId
                : null;

            var amountResult = await paymentService.GetFullPrice(apartmentId, request.CheckInDate, request.CheckOutDate);
            if (!amountResult.Success) return BadRequest(new { Error = amountResult.Error });

            var result = await paymentService.ProcessCardPayment(request, apartmentId, Guid.NewGuid().ToString(), IsSign: false, amountResult.Value, userId); // fix idempotency
            return result.ToActionResult();
        }

        [HttpPost("process_sign_payment/{apartmentId}")]
        [Authorize]
        public async Task<ActionResult<PaymentWithUserEmailDto>> ProcessSignPayment([FromBody] CreatePaymentRequest request, Guid apartmentId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized();

            if (!Guid.TryParse(userId, out Guid userGuid)) return Unauthorized();

            var amountResult = await paymentService.GetSignPrice(apartmentId, request.CheckInDate, request.CheckOutDate);
            if (!amountResult.Success) return BadRequest(new { Error = amountResult.Error });

            // fix idempotency
            var result = await paymentService.ProcessCardPayment(request, apartmentId, Guid.NewGuid().ToString(), IsSign: true, amountResult.Value, userGuid);
            if (!result.Success) return result.ToActionResult();

            return result.ToActionResult();
        }

        [HttpPost("process_rest_payment/{reservationId}")]
        [Authorize]
        public async Task<ActionResult<PaymentWithUserEmailDto>> ProcessRestPayment([FromBody] CreatePaymentRequest request, Guid reservationId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized();

            if (!Guid.TryParse(userId, out Guid userGuid)) return Unauthorized();

            // shouldnt do this
            var reservation = await _db.Reservations.FirstOrDefaultAsync(r => r.Id == reservationId);

            var amountResult = await paymentService.GetRestPrice(reservation.ApartmentId, request.CheckInDate, request.CheckOutDate, reservationId);
            if (!amountResult.Success) return BadRequest(new { Error = amountResult.Error });

            var result = await paymentService.ProcessCardPayment(request, reservation.ApartmentId, Guid.NewGuid().ToString(), IsSign: false, amountResult.Value, userGuid);
            if (!result.Success) return result.ToActionResult();

            return result.ToActionResult();
        }

        [HttpPost("webhook")]
        public async Task<ActionResult<string>> Webhook([FromBody] PaymentWebhookRequest request)
        {
            /* TODO: VALIDAR FIRMA DE MERCADOPAGO*/
            await paymentService.CardPaymentWebhook(request);
            return Ok();
        }
    }
}
