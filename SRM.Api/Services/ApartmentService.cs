using Microsoft.EntityFrameworkCore;
using SRM.Api.Data;
using SRM.Api.Models.Dto.Apartment;
using SRM.Api.Repositories;
using SRM.Api.Repositories.Interfaces;
using SRM.Api.Services.Interfaces;
using SRM.Api.Utils;

namespace SRM.Api.Services
{
    public class ApartmentService(IApartmentRepository apartmentRepository) : IApartmentService
    {
        public async Task<Result<List<ApartmentListingDto>>> GetAll(int limit = 10)
        {
            var apartments = await apartmentRepository.GetApartments();
            return Result<List<ApartmentListingDto>>.Ok(apartments);
        }

        public async Task<Result<ApartmentDetailsDto>> GetById(Guid id)
        {
            var apartment = await apartmentRepository.GetById(id);
            if (apartment == null)
                return Result<ApartmentDetailsDto>.Fail("No se encontro el departamento", 404);

            return Result<ApartmentDetailsDto>.Ok(apartment);
        }
    }
}
