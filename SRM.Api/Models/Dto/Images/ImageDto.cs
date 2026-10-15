namespace SRM.Api.Models.Dto.Images
{
    public record ImageDto(
        Guid Id,
        string Url,
        Guid ApartmentId
    );
}
