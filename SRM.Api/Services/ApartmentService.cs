using Microsoft.EntityFrameworkCore;
using SRM.Api.Data;
using SRM.Api.Models.Dto.Apartment;
using SRM.Api.Services.Interfaces;
using SRM.Api.Utils;

namespace SRM.Api.Services
{
    public class ApartmentService(AppDbContext _db) : IApartmentService
    {
        public async Task<Result<List<ApartmentListingDto>>> GetAll(int limit = 10)
        {
            var apartments = await _db.Apartments
                .AsNoTracking()
                .Select(apartment => new ApartmentListingDto(
                    apartment.Id,
                    apartment.Name,
                    apartment.CoverImgUrl,
                    apartment.Location,
                    apartment.Price
                ))
                .Take(limit)
                .ToListAsync();
            return Result<List<ApartmentListingDto>>.Ok(apartments);
        }

        public async Task<Result<ApartmentDetailsDto>> GetById(Guid id)
        {
            var apartment = await _db.Apartments
                .AsNoTracking()
                .Where(a => a.Id == id)
                .Select(a => new ApartmentDetailsDto(
                    a.Id,
                    a.Name,
                    a.Description,
                    a.Price,
                    a.Location,
                    a.CoverImgUrl,
                    a.Latitude,
                    a.Longitude,
                    a.Images.Select(i => new Models.Dto.Images.ImageDto(
                        i.Id,
                        i.Url,
                        i.ApartmentId
                    )).ToList()
                ))
                .FirstOrDefaultAsync();

            if (apartment == null)
                return Result<ApartmentDetailsDto>.Fail("No se encontro el departamento", 404);

            return Result<ApartmentDetailsDto>.Ok(apartment);
        }
    }
}
