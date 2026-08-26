using SRM.Api.Models.Dto.Apartment;
using SRM.Api.Models.Entities;

namespace SRM.Api.Repositories.Interfaces
{
    public interface IApartmentRepository
    {
        Task<List<ApartmentListingDto>> GetApartments(int limit = 10);
        Task<ApartmentDetailsDto?> GetById(Guid id);
    }
}
