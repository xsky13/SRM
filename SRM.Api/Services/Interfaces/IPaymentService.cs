using MercadoPago.Resource.Payment;
using SRM.Api.Models.Dto.Payment;
using SRM.Api.Utils;

namespace SRM.Api.Services.Interfaces
{
    public interface IPaymentService
    {
        Task<string> CreatePreference(string title, decimal unitPrice);
        Task<Result<PaymentWithUserEmailDto>> ProcessCardPayment(CreatePaymentRequest request, Guid apartmentId, string idempotencyKey);
        Task CardPaymentWebhook(PaymentWebhookRequest request);
    }
}
