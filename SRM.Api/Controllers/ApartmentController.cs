using Microsoft.AspNetCore.Mvc;
using SRM.Api.Models.Dto.Apartment;
using SRM.Api.Services.Interfaces;
using SRM.Api.Utils;

namespace SRM.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ApartmentController(IApartmentService _apartmentService) : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<List<ApartmentListingDto>>> GetAll()
        {
            var response = await _apartmentService.GetAll();
            return response.ToActionResult();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ApartmentDetailsDto>> GetById(Guid id)
        {
            var response = await _apartmentService.GetById(id);
            return response.ToActionResult();
        }
    }
}
