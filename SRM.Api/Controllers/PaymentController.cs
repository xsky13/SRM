using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SRM.Api.Data;
using SRM.Api.Models.Dto.Payment;
using SRM.Api.Models.Entities;
using SRM.Api.Services.Interfaces;
using SRM.Api.Utils;
using System.Security.Claims;

namespace SRM.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentController(IPaymentService paymentService) : ControllerBase
    {
        [HttpPost("process_card_payment/{apartmentId}")]
        public async Task<ActionResult<PaymentWithUserEmailDto>> ProcessFullPayment([FromBody] CreatePaymentRequest request, Guid apartmentId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            Guid? userId = Guid.TryParse(userIdClaim, out var parsedUserId)
                ? parsedUserId
                : null;

            var result = await paymentService.ProcessFullPayment(request, apartmentId, Guid.NewGuid().ToString(), userId); // fix idempotency
            return result.ToActionResult();
        }

        [HttpPost("process_sign_payment/{apartmentId}")]
        [Authorize]
        public async Task<ActionResult<PaymentWithUserEmailDto>> ProcessSignPayment([FromBody] CreatePaymentRequest request, Guid apartmentId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized();

            if (!Guid.TryParse(userId, out Guid userGuid)) return Unauthorized();

            // fix idempotency
            var result = await paymentService.ProcessSignPayment(request, apartmentId, Guid.NewGuid().ToString(), userGuid);
            return result.ToActionResult();
        }

        [HttpPost("process_rest_payment/{reservationId}")]
        [Authorize]
        public async Task<ActionResult<PaymentWithUserEmailDto>> ProcessRestPayment([FromBody] CreatePaymentRequest request, Guid reservationId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized();

            if (!Guid.TryParse(userId, out Guid userGuid)) return Unauthorized();

            var result = await paymentService.ProcessRestPayment(request, reservationId, Guid.NewGuid().ToString(), userGuid);
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
