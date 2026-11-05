namespace SRM.Api.Models.Dto.Payment
{
    public record CreatePreferenceRequest(
        string Title,
        int UnitPrice
    );
}
