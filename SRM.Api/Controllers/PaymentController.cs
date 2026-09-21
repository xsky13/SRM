using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SRM.Api.Models.Dto.Payment;
using SRM.Api.Services.Interfaces;
using SRM.Api.Utils;
using System.Security.Claims;
using System.Text.Json;

namespace SRM.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentController(IPaymentService paymentService, ILogger<GlobalExceptionHandler> logger) : ControllerBase
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

            var result = await paymentService.ProcessCardPayment(request, apartmentId, Guid.NewGuid().ToString(), userId); // fix idempotency
            return result.ToActionResult();
        }

        [HttpPost("process_sign_payment/{apartmentId}")]
        [Authorize]
        public async Task<ActionResult<PaymentWithUserEmailDto>> ProcessSignPayment([FromBody] CreatePaymentRequest request, Guid apartmentId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized();

            if (!Guid.TryParse(userId, out Guid userGuid)) return Unauthorized();

            var result = await paymentService.ProcessSignPayment(request, apartmentId, Guid.NewGuid().ToString(), userGuid); // fix idempotency
            return result.ToActionResult();
        }

        [HttpPost("webhook")]
        public async Task<ActionResult<string>> Webhook([FromBody] PaymentWebhookRequest request)
        {
            await paymentService.CardPaymentWebhook(request);
            return Ok();
        }
    }
}
