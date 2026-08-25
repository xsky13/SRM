namespace SRM.Api.Models.Dto
{
    public record ApartmentDto(
        Guid Id,
        string Name,
        string Description,
        float Price,
        string Location,
        bool IsDeleted
    );
}
