using MercadoPago.Resource.Payment;
using SRM.Api.Models.Dto.Payment;
using SRM.Api.Utils;

namespace SRM.Api.Services.Interfaces
{
    public interface IPaymentService
    {
        Task CardPaymentWebhook(PaymentWebhookRequest request);

        Task<Result<PaymentWithUserEmailDto>> ProcessFullPayment(CreatePaymentRequest request, Guid apartmentId, string idempotencyKey, Guid? userId = null);
        Task<Result<PaymentWithUserEmailDto>> ProcessSignPayment(CreatePaymentRequest request, Guid apartmentId, string idempotencyKey, Guid userId);
        Task<Result<PaymentWithUserEmailDto>> ProcessRestPayment(CreatePaymentRequest request, Guid reservationId, string idempotencyKey, Guid userId);
    }
}
