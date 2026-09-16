using MercadoPago.Client;
using MercadoPago.Client.Common;
using MercadoPago.Client.Payment;
using MercadoPago.Client.Preference;
using MercadoPago.Resource.Preference;
using Microsoft.EntityFrameworkCore;
using SRM.Api.Data;
using SRM.Api.Data.Migrations;
using SRM.Api.Models.Dto.Payment;
using SRM.Api.Models.Entities;
using SRM.Api.Models.Enums;
using SRM.Api.Services.Interfaces;
using SRM.Api.Utils;

namespace SRM.Api.Services
{
    public class PaymentService(AppDbContext _db, ILogger<GlobalExceptionHandler> logger) : IPaymentService
    {
        public async Task<string> CreatePreference(string title, decimal unitPrice)
        {
            var request = new PreferenceRequest
            {
                Items = new List<PreferenceItemRequest>
                {
                    new PreferenceItemRequest
                    {
                        Title = title,
                        Quantity = 1,
                        UnitPrice = unitPrice,
                        CurrencyId = "ARS"
                    }
                }
            };

            var client = new PreferenceClient();
            Preference preference = await client.CreateAsync(request);

            return preference.Id;
        }

        public async Task CardPaymentWebhook(PaymentWebhookRequest request)
        {
            // se llama el webhook con el id de mercado pago
            // conseguir el pago con ese id
            var payment = await _db.Payments.FirstOrDefaultAsync(p => p.MpPaymentId == request.Data.Id);
            if (payment == null)
            {
                logger.LogWarning("Pago con el id no existe: {PaymentId}", request.Data.Id);
                return;
            }

            // en base al estado de este actualizar el estado del pago
            // convertirlo a long porque los pagos de mercado pago trabajan con long
            if (!long.TryParse(request.Data.Id, out var mpPaymentId))
            {
                logger.LogWarning("Webhook con data.id no numérico: {DataId}", request.Data.Id);
                return;
            }

            // actualizar estado del pago
            var client = new PaymentClient();
            var mpPayment = await client.GetAsync(mpPaymentId);

            var newStatus = PaymentStatusMapper.MapMercadoPagoStatus(mpPayment.Status);
            payment.PaymentStatus = newStatus;

            // finalmente tambien actualizar el estado de la reserva

            var reservation = await _db.Reservations.FindAsync(payment.ReservationId);
            if (reservation != null)
            {
                // actualizar el estado de la reserva tambien
                reservation.State = newStatus switch
                {
                    PaymentStatus.Approved => ReservationState.ConfirmedPaymentComplete,
                    PaymentStatus.Rejected or PaymentStatus.Cancelled => ReservationState.Cancelled,
                    _ => reservation.State
                };
                reservation.UpdatedAt = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();

            // mandar ticket si no lo hay
            if (payment.TicketId == null)
            {
                // TODO: llamar servicio de ticket
            }

        }


        public async Task<Result<PaymentDto>> ProcessCardPayment(CreatePaymentRequest request, Guid apartmentId, string idempotencyKey)
        {
            // validaciones
            if (request.CheckOutDate.Date < request.CheckInDate.Date)
                return Result<PaymentDto>.Fail("La fecha de checkout no puede ser anterior a la de checkin");

            decimal apartmentCost = await _db.Apartments
                .Where(a => a.Id == apartmentId)
                .Select(a => a.Price)
                .FirstOrDefaultAsync();

            if (apartmentCost == 0) // no se encontro el departamento
                return Result<PaymentDto>.Fail("El departamento no existe.");

            var diff = (request.CheckOutDate.Date - request.CheckInDate.Date).Days;
            var nights = Math.Max(diff, 1);
            var fullCost = nights * apartmentCost;

            /*Pasos*/
            // crear usuario guest
            var user = new AppUser
            {
                Id = Guid.NewGuid(),
                Name = "",
                LastName = "",
                Email = request.Payer.Email,
                Telefono = "",
                Usertype = UserType.Guest
            };

            // crear reserva con estado payment pending
            var reservation = new Reservation
            {
                Id = Guid.NewGuid(),
                CheckInDate = request.CheckInDate,
                CheckOutDate = request.CheckOutDate,
                State = ReservationState.PaymentPending,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                ApartmentId = apartmentId,
                AppUserId = user.Id
            };

            // fijarse si las fechas seleccionadas colisionan con alguna confirmada o que tiene pago pendiente.
            var datesInvalid = await _db.Reservations.AnyAsync(r =>
                (r.State == ReservationState.ConfirmedPaymentComplete || r.State == ReservationState.ConfirmedPaymentIncomplete || r.State == ReservationState.PaymentPending) &&
                r.CheckInDate < (request.CheckOutDate == request.CheckInDate ? request.CheckOutDate.AddDays(1) : request.CheckOutDate) &&
                (r.CheckOutDate == r.CheckInDate ? r.CheckOutDate.AddDays(1) : r.CheckOutDate) > request.CheckInDate
            );
            if (datesInvalid) return Result<PaymentDto>.Fail("Ya hay una reserva en esta fecha.");

            _db.AppUsers.Add(user);
            _db.Reservations.Add(reservation);
            await _db.SaveChangesAsync();

            // procesar pago

            var requestOptions = new RequestOptions();
            requestOptions.CustomHeaders.Add("x-idempotency-key", idempotencyKey);

            var paymentRequest = new PaymentCreateRequest
            {
                TransactionAmount = fullCost,
                Token = request.Token,
                Description = $"Pago para reserva {reservation.Id}",
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
                logger.LogError(ex, "Error al procesar pago con Mercado Pago para reserva {ReservationId}", reservation.Id);
                reservation.State = ReservationState.Cancelled;
                await _db.SaveChangesAsync();
                return Result<PaymentDto>.Fail("No se pudo procesar el pago");
            }

            var paymentStatus = PaymentStatusMapper.MapMercadoPagoStatus(mpPayment.Status);

            // crear entidad de pago con estado de pending
            var dbPayment = new Payment
            {
                Amount = fullCost,
                IsManual = false,
                IsSign = false,
                PaymentDate = DateTime.UtcNow,
                ReservationId = reservation.Id,
                AppUserId = user.Id,
                PaymentStatus = paymentStatus,
                MpPaymentId = mpPayment.Id.ToString()
            };

            reservation.State = paymentStatus switch
            {
                PaymentStatus.Approved => ReservationState.ConfirmedPaymentComplete,
                PaymentStatus.Rejected or PaymentStatus.Cancelled => ReservationState.Cancelled,
                _ => ReservationState.PaymentPending
            };
            reservation.UpdatedAt = DateTime.UtcNow;

            _db.Payments.Add(dbPayment);
            await _db.SaveChangesAsync();

            if (paymentStatus == PaymentStatus.Approved)
            {
                // llamar servicio de ticket
                
            }

            // retornar success? y esperar que se llame al webhook para la confirmacion del 
            return Result<PaymentDto>.Ok(new PaymentDto(dbPayment.Id, dbPayment.Amount, dbPayment.IsManual, dbPayment.IsSign, dbPayment.PaymentDate, dbPayment.PaymentStatus, dbPayment.ReservationId, dbPayment.AppUserId, null));
        }

    }
}
