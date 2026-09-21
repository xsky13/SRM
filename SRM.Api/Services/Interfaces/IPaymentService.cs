using MercadoPago.Resource.Payment;
using SRM.Api.Models.Dto.Payment;
using SRM.Api.Utils;

namespace SRM.Api.Services.Interfaces
{
    public interface IPaymentService
    {
        Task<string> CreatePreference(string title, decimal unitPrice);
        Task<Result<PaymentWithUserEmailDto>> ProcessCardPayment(CreatePaymentRequest request, Guid apartmentId, string idempotencyKey, Guid? userId);
        Task CardPaymentWebhook(PaymentWebhookRequest request);
        Task<Result<PaymentWithUserEmailDto>> ProcessSignPayment(CreatePaymentRequest request, Guid apartmentId, string idempotencyKey, Guid userId);
    }
}
