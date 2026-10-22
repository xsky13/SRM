using MercadoPago.Client.Payment;
using Microsoft.EntityFrameworkCore;
using SRM.Api.Data;
using SRM.Api.Models.Dto.Payment;
using SRM.Api.Models.Entities;
using SRM.Api.Models.Enums;
using SRM.Api.Services.Interfaces;
using SRM.Api.Utils;

namespace SRM.Api.Services
{
    public class PaymentService(AppDbContext _db, IReservationService _reservationService, IBankService _bankService, IUserService _userService, ILogger<GlobalExceptionHandler> logger) : IPaymentService
    {
        public async Task CardPaymentWebhook(PaymentWebhookRequest request)
        {
            if (request.Type != "payment") return;

            // en base al estado de este actualizar el estado del pago
            // convertirlo a long porque los pagos de mercado pago trabajan con long
            if (!long.TryParse(request.Data.Id, out var mpPaymentId))
            {
                logger.LogWarning("Webhook con data.id no numérico: {DataId}", request.Data.Id);
                return;
            }

            // conseguir el pago con ese id
            var payment = await _db.Payments.FirstOrDefaultAsync(p => p.MpPaymentId == request.Data.Id);
            if (payment == null)
            {
                logger.LogWarning("Pago con el id no existe: {PaymentId}", request.Data.Id);
                throw new KeyNotFoundException($"Pago {request.Data.Id} no encontrado"); // supuestamente con esto mp reintenta el pago porque es 404??
            }

            // actualizar estado del pago
            var client = new PaymentClient();
            var mpPayment = await client.GetAsync(mpPaymentId);

            var newStatus = PaymentStatusMapper.MapMercadoPagoStatus(mpPayment.Status);
            if (payment.PaymentStatus == newStatus) return;
            payment.PaymentStatus = newStatus;

            // finalmente tambien actualizar el estado de la reserva
            var reservation = await _db.Reservations.FindAsync(payment.ReservationId);
            if (reservation != null)
            {
                // hay que verificar si al hacer el pago hay que actualizar las fechas de la reserva, o su estado (con payment.PreviousReservationState)
                // PreviousReservationState se usa en caso de error y tener que hacer fallback
                var isDateChange = payment.PendingCheckInDate.HasValue && payment.PendingCheckOutDate.HasValue;

                switch (newStatus)
                {
                    case PaymentStatus.Approved:
                        if (isDateChange)
                {
                            reservation.CheckInDate = payment.PendingCheckInDate!.Value;
                            reservation.CheckOutDate = payment.PendingCheckOutDate!.Value;
                            reservation.State = ReservationState.ConfirmedPaymentComplete;
                }
                        else if (payment.IsSign)
                        {
                            if (reservation.State != ReservationState.ConfirmedPaymentComplete)
                                reservation.State = ReservationState.ConfirmedPaymentIncomplete;
                        }
                        else reservation.State = ReservationState.ConfirmedPaymentComplete; // el pago es lo restante, se puede poner como confirmado completo

                        break;
                    case PaymentStatus.Rejected or PaymentStatus.Cancelled:
                        // si estamos tratando de modificar la reserva y se rechaza el pago, volvemos al estado anterior de la reserva
                        reservation.State = payment.PreviousReservationState ?? (isDateChange ? ReservationState.ConfirmedPaymentIncomplete : ReservationState.Cancelled);
                        break;
                    case PaymentStatus.Refunded or PaymentStatus.ChargedBack or PaymentStatus.InMediation:
                        logger.LogError("Pago {PaymentId} paso a {Status} (reserva {ReservationId}): requiere revision manual", payment.Id, newStatus, reservation.Id);
                        // TODO: avisar al admin, decidir que hacer con la reserva
                        break;
                }

                if (isDateChange && (newStatus is PaymentStatus.Approved or PaymentStatus.Rejected or PaymentStatus.Cancelled))
                {
                    payment.PendingCheckInDate = null;
                    payment.PendingCheckOutDate = null;
                    payment.PreviousReservationState = null;
                }

                reservation.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

                if (newStatus == PaymentStatus.Approved && payment.TicketId == null)
            {
                // TODO: llamar servicio de ticket
            }
        }
        }


        public async Task<Result<PaymentWithUserEmailDto>> ProcessFullPayment(CreatePaymentRequest request, Guid apartmentId, string idempotencyKey, Guid? userId = null, Guid? reservationId = null)
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

            // crear reserva con estado payment pending (si no existe todavia). ya verifica si las fechas son validas o no
            Reservation reservation;
            if (reservationId.HasValue)
            {
                var reservationQuery = await _db.Reservations.FindAsync(reservationId);
                if (reservationQuery == null) return Result<PaymentWithUserEmailDto>.Fail("No se encontro la reserva");
                reservation = reservationQuery;
            } else
            {
                var reservationResult = await _reservationService.CreateReservation(request.CheckInDate, request.CheckOutDate, apartmentId, userIdToUse);
                if (!reservationResult.Success) return Result<PaymentWithUserEmailDto>.Fail(reservationResult.Error!);
                reservation = reservationResult.Value;
            }

            // validar que no haya ningun pago pendiente
            if (reservation.State == ReservationState.PaymentPending)
                return Result<PaymentWithUserEmailDto>.Fail("Hay pagos pendientes para esta reserva.");

            await _db.SaveChangesAsync(); // guardar los datos en la base de datos para que nadie entre con los mismos datos antes

            var paymentResult = await _bankService.CreatePayment(request, amount, userIdToUse, reservation.Id, idempotencyKey, isSign: false);
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

        public async Task<Result<PaymentWithUserEmailDto>> ProcessSignPayment(CreatePaymentRequest request, Guid apartmentId, string idempotencyKey, Guid userId, Guid? reservationId = null)
        {
            if (request.CheckOutDate.Date < request.CheckInDate.Date)
                return Result<PaymentWithUserEmailDto>.Fail("La fecha de checkout no puede ser anterior a la de checkin");

            // conseguir monto
            var amountResult = await GetSignPrice(apartmentId, request.CheckInDate, request.CheckOutDate);
            if (!amountResult.Success) return Result<PaymentWithUserEmailDto>.Fail(amountResult.Error);
            var amount = amountResult.Value;

            /*Pasos*/

            // crear reserva con estado payment pending. ya verifica si las fechas son validas o no
            Reservation reservation;
            if (reservationId.HasValue)
            {
                var reservationQuery = await _db.Reservations.FindAsync(reservationId);
                if (reservationQuery == null) return Result<PaymentWithUserEmailDto>.Fail("No se encontro la reserva");
                reservation = reservationQuery;

                // just in case
                // validar que no haya ningun pago pendiente
                if (reservation.State == ReservationState.PaymentPending)
                    return Result<PaymentWithUserEmailDto>.Fail("Hay pagos pendientes para esta reserva.");

                // check for conflicting dates
                var conflicting = await _reservationService.DatesAreInvalid(reservation.CheckOutDate, reservation.CheckInDate, reservation.ApartmentId);
                if (conflicting)
                    return Result<PaymentWithUserEmailDto>.Fail("Ya hay una reserva con estas fechas");

                reservation.State = ReservationState.ConfirmedPaymentIncomplete; // lock down the reservation dates so nobody else takes them.
            }
            else
            {
                var reservationResult = await _reservationService.CreateReservation(request.CheckInDate, request.CheckOutDate, apartmentId, userId);
                if (!reservationResult.Success) return Result<PaymentWithUserEmailDto>.Fail(reservationResult.Error!);
                reservation = reservationResult.Value;
            }

            await _db.SaveChangesAsync(); // guardar los datos en la base de datos para que nadie entre con los mismos datos antes

            var paymentResult = await _bankService.CreatePayment(request, amount, userId, reservation.Id, idempotencyKey, isSign: true);
            if (!paymentResult.Success)
            {
                reservation.State = ReservationState.NotConfirmed;
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

            // validar que no haya ningun pago pendiente
            if (reservation.State == ReservationState.PaymentPending)
                return Result<PaymentWithUserEmailDto>.Fail("Hay pagos pendientes para esta reserva.");

            ReservationState prevReservationState = reservation.State;

            var paymentResult = await _bankService.CreatePayment(request, amount, userId, reservation.Id, idempotencyKey, isSign: false);
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
