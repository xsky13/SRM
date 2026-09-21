using MercadoPago.Resource.Payment;
using SRM.Api.Models.Dto.Payment;
using SRM.Api.Utils;

namespace SRM.Api.Services.Interfaces
{
    public interface IPaymentService
    {
        Task<string> CreatePreference(string title, decimal unitPrice);
        Task<Result<PaymentWithUserEmailDto>> ProcessCardPayment(CreatePaymentRequest request, Guid apartmentId, string idempotencyKey, bool IsSign, decimal amount, Guid? userId);
        Task CardPaymentWebhook(PaymentWebhookRequest request);

        Task<Result<decimal>> GetFullPrice(Guid apartmentId, DateTime checkInDate, DateTime checkOutDate);
        Task<Result<decimal>> GetSignPrice(Guid apartmentId, DateTime checkInDate, DateTime checkOutDate);
        Task<Result<decimal>> GetRestPrice(Guid apartmentId, DateTime checkInDate, DateTime checkOutDate, Guid reservationId);
    }
}
