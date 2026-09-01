using Microsoft.AspNetCore.Mvc;
using SRM.Api.Models.Dto.Payment;
using SRM.Api.Services.Interfaces;

namespace SRM.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentController(IPaymentService paymentService) : ControllerBase
    {
        [HttpPost("preference")]
        public async Task<ActionResult<string>> CreatePreference([FromBody] CreatePreferenceRequest request)
        {
            string preferenceId = await paymentService.CreatePreference(request.Title, request.UnitPrice);
            return Ok(preferenceId);
        }
    }
}
