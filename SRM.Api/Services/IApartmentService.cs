
using SRM.Api.Models.Dto;
using SRM.Api.Utils;

namespace SRM.Api.Services
{
    public interface IApartmentService
    {
        Task<Result<ApartmentDto[]>> GetAll(int limit = 10);
    }
}
