using MercadoPago.Client;
using MercadoPago.Client.Common;
using MercadoPago.Client.Payment;
using Microsoft.EntityFrameworkCore;
using SRM.Api.Data;
using SRM.Api.Models.Dto.Payment;
using SRM.Api.Models.Entities;
using SRM.Api.Services.Interfaces;
using SRM.Api.Utils;

namespace SRM.Api.Services
{
    public class MpBankService(AppDbContext _db, ILogger<GlobalExceptionHandler> logger) : IBankService
    {
        /*
  * IMPORTANTE: SIEMPRE LLAMAR db.SaveChanges DESPUES DE USAR ESTA FUNCION.
  */
        public async Task<Result<Payment>> CreatePayment(CreatePaymentRequest request, decimal amount, Guid userId, Guid reservationId, bool isSign)
        {
            var attempt = await _db.Payments.CountAsync(p => p.ReservationId == reservationId);
            var key = $"{reservationId}_{(isSign ? "sign" : "pay")}_{attempt}";

            var requestOptions = new RequestOptions();
            requestOptions.CustomHeaders.Add("x-idempotency-key", key);

            var paymentRequest = new PaymentCreateRequest
            {
                TransactionAmount = amount,
                Token = request.Token,
                Description = $"Pago para reserva {reservationId}",
                Installments = request.Installments,
                PaymentMethodId = request.Payment_Method_Id,
                Payer = new PaymentPayerRequest
                {
                    Email = request.Payer.Email,
                    Identification = new IdentificationRequest
                    {
                        Type = request.Payer.Identification.Type,
                        Number = request.Payer.Identification.Number,
                    },
                    FirstName = ""
                },
            };

            var client = new PaymentClient();
            MercadoPago.Resource.Payment.Payment mpPayment;

            try
            {
                mpPayment = await client.CreateAsync(paymentRequest, requestOptions);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error al procesar pago con Mercado Pago para reserva {ReservationId}", reservationId);
                return Result<Payment>.Fail("No se pudo procesar el pago");
            }

            var paymentStatus = PaymentStatusMapper.MapMercadoPagoStatus(mpPayment.Status);
            var dbPayment = new Payment
            {
                Amount = amount,
                IsManual = false,
                IsSign = isSign,
                PaymentDate = DateTime.UtcNow,
                ReservationId = reservationId,
                AppUserId = userId,
                PaymentStatus = paymentStatus,
                MpPaymentId = mpPayment.Id.ToString()
            };
            _db.Payments.Add(dbPayment);

            return Result<Payment>.Ok(dbPayment);
        }

        public async Task<string> GetAsync(long mpPaymentId)
        {
            var client = new PaymentClient();
            var mpPayment = await client.GetAsync(mpPaymentId);
            return mpPayment.Status;
        }
    }
}
