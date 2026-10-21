using SRM.Api.Models.Dto.Apartment;
using SRM.Api.Models.Entities;
using SRM.Api.Utils;

namespace SRM.Api.Services.Interfaces
{
    public interface IApartmentService
    {
        Task<Result<List<ApartmentListingDto>>> GetAll(int limit = 10);
        Task<Result<ApartmentDetailsDto>> GetById(Guid id);
        Task<Result<Apartment>> CreateApartmentAsync(CreateApartmentDto dto);
        Task<Result<Apartment>> DeleteApartmentAsync(Guid id);
    }
}
