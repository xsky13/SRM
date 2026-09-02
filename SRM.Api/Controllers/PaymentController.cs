using Microsoft.AspNetCore.Mvc;
using SRM.Api.Models.Dto.Payment;
using SRM.Api.Services.Interfaces;
using SRM.Api.Utils;
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
        public async Task<ActionResult<PaymentDto>> ProcessPayment([FromBody] CreatePaymentRequest request, Guid apartmentId)
        {
            var result = await paymentService.ProcessCardPayment(request, apartmentId, Guid.NewGuid().ToString()); // fix idempotency
            return result.ToActionResult();
        }

        [HttpPost("webhook")]
        public ActionResult<string> Webhook([FromBody] PaymentWebhookRequest request)
        {
            string json = JsonSerializer.Serialize(request);
            Console.WriteLine(json);
            logger.LogInformation("{@json}", json);
            return Ok(json);
        }
    }
}
