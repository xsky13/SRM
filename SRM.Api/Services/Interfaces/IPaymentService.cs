namespace SRM.Api.Services.Interfaces
{
    public interface IPaymentService
    {
        Task<string> CreatePreference(string title, int unitPrice);
    }
}
