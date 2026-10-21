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

        [Authorize]
        [HttpPut("{id}")]
        public async Task<ActionResult<Apartment>> UpdateApartment(Guid id, [FromBody] UpdateApartmentDto request)
        {
            // Validar que el usuario sea administrador
            if (!User.IsInRole(UserType.Admin.ToString()))
                return Forbid();

            // Llamar al servicio
            var response = await _apartmentService.UpdateApartmentAsync(id, request);
            return response.ToActionResult();
        }

        [Authorize]
        [HttpPost("{id}/images")]
        public async Task<ActionResult<List<Models.Dto.Images.ImageDto>>> AddImages(Guid id, [FromBody] AddImagesDto request)
        {
            // Validar que el usuario sea administrador
            if (!User.IsInRole(UserType.Admin.ToString()))
                return Forbid();

            // Llamar al servicio
            var response = await _apartmentService.AddImagesAsync(id, request);
            return response.ToActionResult();
        }

        [Authorize]
        [HttpDelete("{id}/images")]
        public async Task<ActionResult<bool>> RemoveImages(Guid id, [FromBody] RemoveImagesDto request)
        {
            // Validar que el usuario sea administrador
            if (!User.IsInRole(UserType.Admin.ToString()))
                return Forbid();

            // Llamar al servicio
            var response = await _apartmentService.RemoveImagesAsync(id, request);
            return response.ToActionResult();
        }


    }
}
