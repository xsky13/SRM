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
    public class PaymentService(AppDbContext _db, IReservationService _reservationService, IUserService _userService, ILogger<GlobalExceptionHandler> logger) : IPaymentService
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

                // buscar pagos 
                var payments = await _db.Payments
                    .Where(p => p.ReservationId == reservation.Id)
                    .Select(p => new { p.IsSign, p.PaymentStatus })
                    .ToListAsync();

                // si no hay ninguno que no sea sena, el pago esta incompleto
                if (!payments.Any(p => !p.IsSign && p.PaymentStatus == PaymentStatus.Approved))
                {
                    reservation.State = newStatus switch
                    {
                        PaymentStatus.Approved => ReservationState.ConfirmedPaymentIncomplete,
                        PaymentStatus.Rejected or PaymentStatus.Cancelled => ReservationState.Cancelled,
                        _ => reservation.State
                    };
                }

                reservation.UpdatedAt = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();

            // mandar ticket si no lo hay
            if (payment.TicketId == null)
            {
                // TODO: llamar servicio de ticket
            }

        }

        public async Task<Result<PaymentWithUserEmailDto>> ProcessCardPayment(CreatePaymentRequest request, Guid apartmentId, string idempotencyKey, bool IsSign, decimal amount, Guid? userId = null)
        {
            if (request.CheckOutDate.Date < request.CheckInDate.Date)
                return Result<PaymentWithUserEmailDto>.Fail("La fecha de checkout no puede ser anterior a la de checkin");

            /*Pasos*/
            // crear usuario guest si no nos dan id
            Guid userIdToUse;
            if (userId.HasValue) userIdToUse = userId.Value;
            else
            {
                var user = _userService.CreateUser(email: request.Payer.Email);
                userIdToUse = user.Id;
            }

            // crear reserva con estado payment pending. ya verifica si las fechas son validas o no
            var reservationResult = await _reservationService.CreateReservation(request.CheckInDate, request.CheckOutDate, apartmentId, userIdToUse);
            if (!reservationResult.Success) return Result<PaymentWithUserEmailDto>.Fail(reservationResult.Error!);

            var reservation = reservationResult.Value;

            await _db.SaveChangesAsync();

            // procesar pago

            var requestOptions = new RequestOptions();
            requestOptions.CustomHeaders.Add("x-idempotency-key", idempotencyKey);

            var paymentRequest = new PaymentCreateRequest
            {
                TransactionAmount = amount,
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
                return Result<PaymentWithUserEmailDto>.Fail("No se pudo procesar el pago");
            }

            var paymentStatus = PaymentStatusMapper.MapMercadoPagoStatus(mpPayment.Status);

            // crear entidad de pago con estado de pending
            var dbPayment = new Payment
            {
                Amount = amount,
                IsManual = false,
                IsSign = IsSign,
                PaymentDate = DateTime.UtcNow,
                ReservationId = reservation.Id,
                AppUserId = userIdToUse,
                PaymentStatus = paymentStatus,
                MpPaymentId = mpPayment.Id.ToString()
            };

            reservation.State = paymentStatus switch
            {
                PaymentStatus.Approved => IsSign ? ReservationState.ConfirmedPaymentIncomplete : ReservationState.ConfirmedPaymentComplete,
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
            return Result<PaymentWithUserEmailDto>.Ok(new PaymentWithUserEmailDto
            {
                Id = dbPayment.Id,
                Amount = dbPayment.Amount,
                IsManual = dbPayment.IsManual,
                IsSign = dbPayment.IsSign,
                PaymentDate = dbPayment.PaymentDate,
                PaymentStatus = dbPayment.PaymentStatus,
                ReservationId = dbPayment.ReservationId,
                AppUserId = dbPayment.AppUserId,
                TicketId = null,
                Email = request.Payer.Email
            });
        }





        public async Task<Result<decimal>> GetFullPrice(Guid apartmentId, DateTime checkInDate, DateTime checkOutDate)
        {
            decimal apartmentCost = await _db.Apartments
                .Where(a => a.Id == apartmentId)
                .Select(a => a.Price)
                .FirstOrDefaultAsync();

            if (apartmentCost == 0) // no se encontro el departamento
                return Result<decimal>.Fail("El departamento no existe.");

            var diff = (checkOutDate.Date - checkInDate.Date).Days;
            var nights = Math.Max(diff, 1);
            var fullCost = nights * apartmentCost;

            return Result<decimal>.Ok(fullCost);
        }

        public async Task<Result<decimal>> GetSignPrice(Guid apartmentId, DateTime checkInDate, DateTime checkOutDate)
        {
            decimal apartmentCost = await _db.Apartments
                .Where(a => a.Id == apartmentId)
                .Select(a => a.Price)
                .FirstOrDefaultAsync();

            if (apartmentCost == 0) // no se encontro el departamento
                return Result<decimal>.Fail("El departamento no existe.");

            var diff = (checkOutDate.Date - checkInDate.Date).Days;
            var nights = Math.Max(diff, 1);
            var fullCost = nights * apartmentCost * 0.1m;

            return Result<decimal>.Ok(fullCost);
        }

        public async Task<Result<decimal>> GetRestPrice(Guid apartmentId, DateTime checkInDate, DateTime checkOutDate, Guid reservationId)
        {
            decimal apartmentCost = await _db.Apartments
                .Where(a => a.Id == apartmentId)
                .Select(a => a.Price)
                .FirstOrDefaultAsync();

            if (apartmentCost == 0) // no se encontro el departamento
                return Result<decimal>.Fail("El departamento no existe.");

            var diff = (checkOutDate.Date - checkInDate.Date).Days;
            var nights = Math.Max(diff, 1);
            var fullCost = nights * apartmentCost;

            // get payed cost
            var payments = await _db.Payments
                .Where(p => p.ReservationId == reservationId)
                .Select(p => new { p.Amount })
                .ToListAsync();

            decimal totalPayed = 0m;
            foreach (var payment in payments)
                totalPayed += payment.Amount;

            if (totalPayed >= fullCost)
                return Result<decimal>.Fail("Ya se pago la reserva.");

            decimal amountToPay = fullCost - totalPayed;

            return Result<decimal>.Ok(amountToPay);
        }

    }
}
