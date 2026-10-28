using SRM.Api.Models.Dto.Payment;
using SRM.Api.Models.Entities;
using SRM.Api.Utils;

namespace SRM.Api.Services.Interfaces
{
    public interface IBankService
    {
        Task<Result<Payment>> CreatePayment(CreatePaymentRequest request, decimal amount, Guid userId, Guid reservationId, string idempotencyKey, bool isSign);
        Task<string> GetAsync(long mpPaymentId);
    }
}
