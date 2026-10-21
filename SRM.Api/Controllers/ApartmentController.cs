using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SRM.Api.Models.Dto.Apartment;
using SRM.Api.Models.Entities;
using SRM.Api.Models.Enums;
using SRM.Api.Services.Interfaces;
using SRM.Api.Utils;
using System.Security.Claims;

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

        [Authorize]
        [HttpPost]
        public async Task<ActionResult<Apartment>> CreateApartment([FromBody] CreateApartmentDto request)
        {
            // Extraer el userId del claim del token
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized();

            // validar que el tipo extraido sea el de un user admin
            if (!User.IsInRole(UserType.Admin.ToString()))
                return Forbid();

            // Llamar al servicio
            var response = await _apartmentService.CreateApartmentAsync(request);
            return response.ToActionResult();
        }

        [Authorize]
        [HttpDelete("{id}")]
        public async Task<ActionResult<Apartment>> DeleteApartment(Guid id)
        {
            // Validar que el usuario sea administrador
            if (!User.IsInRole(UserType.Admin.ToString()))
                return Forbid();

            // Llamar al servicio para eliminar (soft delete)
            var response = await _apartmentService.DeleteApartmentAsync(id);
            return response.ToActionResult();
        }
    }
}
