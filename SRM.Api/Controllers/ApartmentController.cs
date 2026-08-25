using Microsoft.AspNetCore.Mvc;
using SRM.Api.Models.Dto;
using SRM.Api.Services;
using SRM.Api.Utils;

namespace SRM.Api.Controllers
{
    [ApiController]
    [Route("api/[route]")]
    public class ApartmentController(IApartmentService _apartmentService) : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<List<ApartmentDto>>> GetAll()
        {
            var response = await _apartmentService.GetAll();
            return response.ToActionResult();
        }
    }
}
