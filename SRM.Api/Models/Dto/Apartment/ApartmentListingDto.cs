namespace SRM.Api.Models.Dto.Apartment
{
    public record ApartmentListingDto(
        Guid Id,
        string Name,
        string CoverImgUrl,
        string Location,
        float Price
    );
}
