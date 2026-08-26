using Microsoft.EntityFrameworkCore;
using SRM.Api.Data;
using SRM.Api.Models.Dto.Apartment;
using SRM.Api.Repositories.Interfaces;

namespace SRM.Api.Repositories
{
    public class ApartmentRepository(AppDbContext _db) : IApartmentRepository
    {
        public async Task<ApartmentDetailsDto?> GetById(Guid id)
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
                    a.Images.Select(i => new Models.Dto.Images.ImageDto(
                        i.Id, 
                        i.Url, 
                        i.ApartmentId
                    )).ToList()
                ))
                .FirstOrDefaultAsync();
            return apartment;
        }

        public async Task<List<ApartmentListingDto>> GetApartments(int limit = 10)
        {
            var apartments = await _db.Apartments
                .AsNoTracking()
                .Select(apartment => new ApartmentListingDto(
                    apartment.Id,
                    apartment.Name,
                    apartment.Description,
                    apartment.Price
                ))
                .Take(limit)
                .ToListAsync();
            return apartments;
        }
    }
}
