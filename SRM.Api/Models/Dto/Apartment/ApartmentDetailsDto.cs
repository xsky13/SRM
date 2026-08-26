using SRM.Api.Models.Dto.Images;

namespace SRM.Api.Models.Dto.Apartment
{
    public record ApartmentDetailsDto(
        Guid Id,
        string Name,
        string Description,
        float Price,
        string Location,
        double Latitude,
        double Longitude,
        List<ImageDto> Images
    );
}
