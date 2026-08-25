using Microsoft.EntityFrameworkCore;
using SRM.Api.Data;
using SRM.Api.Models.Dto;
using SRM.Api.Utils;

namespace SRM.Api.Services.Impl
{
    public class ApartmentServiceImpl(AppDbContext _db) : IApartmentService
    {
        public async Task<Result<List<ApartmentDto>>> GetAll(int limit = 10)
        {
            var apartments = await _db.Apartments
                .Select(apartment => new ApartmentDto(
                    apartment.Id,
                    apartment.Name,
                    apartment.Description,
                    apartment.Price,
                    apartment.Location,
                    apartment.IsDeleted
                ))
                .Take(limit)
                .AsNoTracking()
                .ToListAsync();

            return Result<List<ApartmentDto>>.Ok(apartments);
        }
    }
}
