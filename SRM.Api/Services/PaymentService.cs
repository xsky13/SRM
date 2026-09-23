using MercadoPago.Client;
using MercadoPago.Client.Common;
using MercadoPago.Client.Payment;
using MercadoPago.Client.Preference;
using MercadoPago.Resource.Preference;
using Microsoft.EntityFrameworkCore;
using SRM.Api.Data;
using SRM.Api.Models.Dto.Payment;
using SRM.Api.Models.Entities;
using SRM.Api.Models.Enums;
using SRM.Api.Services.Interfaces;
using SRM.Api.Utils;

namespace SRM.Api.Services
{
    public class PaymentService(AppDbContext _db, IReservationService _reservationService, IUserService _userService, ILogger<GlobalExceptionHandler> logger) : IPaymentService
    {
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


        /*
         * IMPORTANTE: SIEMPRE LLAMAR db.SaveChanges DESPUES DE USAR ESTA FUNCION.
         */
        public async Task<Result<Payment>> CreatePayment(CreatePaymentRequest request, decimal amount, Guid userId, Guid reservationId, string idempotencyKey, bool isSign)
        {
            var requestOptions = new RequestOptions();
            requestOptions.CustomHeaders.Add("x-idempotency-key", idempotencyKey);

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



        public async Task<Result<PaymentWithUserEmailDto>> ProcessFullPayment(CreatePaymentRequest request, Guid apartmentId, string idempotencyKey, Guid? userId = null)
        {
            if (request.CheckOutDate.Date < request.CheckInDate.Date)
                return Result<PaymentWithUserEmailDto>.Fail("La fecha de checkout no puede ser anterior a la de checkin");

            // conseguir monto
            var amountResult = await GetFullPrice(apartmentId, request.CheckInDate, request.CheckOutDate);
            if (!amountResult.Success) return Result<PaymentWithUserEmailDto>.Fail(amountResult.Error!);
            var amount = amountResult.Value;

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

            await _db.SaveChangesAsync(); // guardar los datos en la base de datos para que nadie entre con los mismos datos antes

            var paymentResult = await CreatePayment(request, amount, userIdToUse, reservation.Id, idempotencyKey, isSign: false);
            if (!paymentResult.Success)
            {
                reservation.State = ReservationState.Cancelled;
                await _db.SaveChangesAsync();
                return Result<PaymentWithUserEmailDto>.Fail(paymentResult.Error!);
            }

            Payment payment = paymentResult.Value!;

            reservation.State = payment.PaymentStatus switch
            {
                PaymentStatus.Approved => ReservationState.ConfirmedPaymentComplete,
                PaymentStatus.Rejected or PaymentStatus.Cancelled => ReservationState.Cancelled,
                _ => ReservationState.PaymentPending
            };
            reservation.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            if (payment.PaymentStatus == PaymentStatus.Approved)
            {
                // llamar servicio de ticket
            }

            var dto = payment.ToDtoWithUserEmail();
            dto.Email = request.Payer.Email;

            return Result<PaymentWithUserEmailDto>.Ok(dto);
        }

        public async Task<Result<PaymentWithUserEmailDto>> ProcessSignPayment(CreatePaymentRequest request, Guid apartmentId, string idempotencyKey, Guid userId)
        {
            if (request.CheckOutDate.Date < request.CheckInDate.Date)
                return Result<PaymentWithUserEmailDto>.Fail("La fecha de checkout no puede ser anterior a la de checkin");

            // conseguir monto
            var amountResult = await GetSignPrice(apartmentId, request.CheckInDate, request.CheckOutDate);
            if (!amountResult.Success) return Result<PaymentWithUserEmailDto>.Fail(amountResult.Error);
            var amount = amountResult.Value;

            /*Pasos*/

            // crear reserva con estado payment pending. ya verifica si las fechas son validas o no
            var reservationResult = await _reservationService.CreateReservation(request.CheckInDate, request.CheckOutDate, apartmentId, userId);
            if (!reservationResult.Success) return Result<PaymentWithUserEmailDto>.Fail(reservationResult.Error!);
            var reservation = reservationResult.Value!;

            await _db.SaveChangesAsync(); // guardar los datos en la base de datos para que nadie entre con los mismos datos antes

            var paymentResult = await CreatePayment(request, amount, userId, reservation.Id, idempotencyKey, isSign: true);
            if (!paymentResult.Success)
            {
                reservation.State = ReservationState.Cancelled;
                await _db.SaveChangesAsync();
                return Result<PaymentWithUserEmailDto>.Fail(paymentResult.Error!);
            }

            Payment payment = paymentResult.Value!;

            reservation.State = payment.PaymentStatus switch
            {
                PaymentStatus.Approved => ReservationState.ConfirmedPaymentIncomplete,
                PaymentStatus.Rejected or PaymentStatus.Cancelled => ReservationState.Cancelled,
                _ => ReservationState.PaymentPending
            };
            reservation.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            if (payment.PaymentStatus == PaymentStatus.Approved)
            {
                // llamar servicio de ticket
            }

            var dto = payment.ToDtoWithUserEmail();
            dto.Email = request.Payer.Email;

            return Result<PaymentWithUserEmailDto>.Ok(dto);
        }

        public async Task<Result<PaymentWithUserEmailDto>> ProcessRestPayment(CreatePaymentRequest request, Guid reservationId, string idempotencyKey, Guid userId)
        {
            if (request.CheckOutDate.Date < request.CheckInDate.Date)
                return Result<PaymentWithUserEmailDto>.Fail("La fecha de checkout no puede ser anterior a la de checkin");

            // conseguir monto
            var amountResult = await GetRestPrice(reservationId);
            if (!amountResult.Success) return Result<PaymentWithUserEmailDto>.Fail(amountResult.Error!);
            var amount = amountResult.Value!;


            //buscar reserva
            var reservation = await _db.Reservations.FirstOrDefaultAsync(r => r.Id == reservationId);
            if (reservation == null) return Result<PaymentWithUserEmailDto>.Fail("No existe la reserva");

            // verificar que reserva pertenezca al usuario
            if (reservation.AppUserId != userId) return Result<PaymentWithUserEmailDto>.Fail("Esta reserva no te pertenece.");

            ReservationState prevReservationState = reservation.State;

            var paymentResult = await CreatePayment(request, amount, userId, reservation.Id, idempotencyKey, isSign: false);
            if (!paymentResult.Success)
            {
                reservation.State = prevReservationState;
                await _db.SaveChangesAsync();
                return Result<PaymentWithUserEmailDto>.Fail(paymentResult.Error!);
            }

            Payment payment = paymentResult.Value!;

            reservation.State = payment.PaymentStatus switch
            {
                PaymentStatus.Approved => ReservationState.ConfirmedPaymentComplete,
                PaymentStatus.Rejected or PaymentStatus.Cancelled => prevReservationState,
                _ => prevReservationState
            };
            reservation.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            if (payment.PaymentStatus == PaymentStatus.Approved)
            {
                // llamar servicio de ticket
            }

            var dto = payment.ToDtoWithUserEmail();
            dto.Email = request.Payer.Email;

            return Result<PaymentWithUserEmailDto>.Ok(dto);
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

        public async Task<Result<decimal>> GetRestPrice(Guid reservationId)
        {
            var reservation = await _db.Reservations
                .Include(r => r.Apartment)
                .FirstOrDefaultAsync(r => r.Id == reservationId);

            if (reservation == null)
                return Result<decimal>.Fail("La reserva no existe.");

            // sacar los dias desde la reserva ya creada porque el usuario podria pasar fechas que acorten la reserva
            var diff = (reservation.CheckOutDate.Date - reservation.CheckInDate.Date).Days;
            var nights = Math.Max(diff, 1);
            var fullCost = nights * reservation.Apartment.Price;

            var totalPaid = await _db.Payments
                .Where(p => p.ReservationId == reservationId && p.PaymentStatus == PaymentStatus.Approved)
                .SumAsync(p => p.Amount);

            if (totalPaid >= fullCost)
                return Result<decimal>.Fail("Ya se pagó la reserva.");

            return Result<decimal>.Ok(fullCost - totalPaid);
        }

    }
}
