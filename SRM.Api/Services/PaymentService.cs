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
                            // editarse una reserva, si setea al estado de PaymentPending, todavia no se actualizaron las fechas, entonces cualquiera puede reservar en ellas
                            var unavailable = await _reservationService.DatesAreInvalid(payment.PendingCheckOutDate!.Value, payment.PendingCheckInDate!.Value, reservation.ApartmentId, reservation.Id);
                            if (unavailable)
                            {
                                // el pago ya se cobro, quedan las fechas viejas y hay que reembolsar a mano
                                logger.LogError("Pago {PaymentId} aprobado pero las fechas nuevas de la reserva {ReservationId} ya no estan disponibles", payment.Id, reservation.Id);
                                reservation.State = payment.PreviousReservationState ?? ReservationState.ConfirmedPaymentIncomplete;
                                // TODO: avisar al admin
                            }
                            else
                            {
                                reservation.CheckInDate = payment.PendingCheckInDate!.Value;
                                reservation.CheckOutDate = payment.PendingCheckOutDate!.Value;
                                reservation.State = ReservationState.ConfirmedPaymentComplete;
                            }
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

                if (newStatus == PaymentStatus.Approved || newStatus == PaymentStatus.Rejected || newStatus == PaymentStatus.Cancelled)
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



        private async Task<Result<Reservation>> GetPayableReservation(Guid reservationId, Guid? userId, ReservationState required)
        {
            var reservation = await _db.Reservations.FirstOrDefaultAsync(r => r.Id == reservationId);
            if (reservation == null) return Result<Reservation>.Fail("No se encontro la reserva", 404);

            if (userId == null || reservation.AppUserId != userId) return Result<Reservation>.Fail("Esta reserva no te pertenece.", 403);
            if (reservation.State != required) return Result<Reservation>.Fail("La reserva no esta en un estado que permita este pago.");

            return Result<Reservation>.Ok(reservation);
        }

        private static void ApplyPaymentOutcome(Reservation reservation, Payment payment, ReservationState approvedState, ReservationState? prevState)
        {
            switch (payment.PaymentStatus)
            {
                case PaymentStatus.Approved:
                    reservation.State = approvedState; // puede ser ConfirmedPaymentIncomplete o ConfirmedPaymentComplete
                    break;
                case PaymentStatus.Rejected or PaymentStatus.Cancelled:
                    reservation.State = prevState ?? ReservationState.Cancelled;
                    break;
                default: // pending, in_process, authorized: el webhook lo resuelve
                    payment.PreviousReservationState = prevState;
                    reservation.State = ReservationState.PaymentPending;
                    break;
            }
            reservation.UpdatedAt = DateTime.UtcNow;
        }


        public async Task<Result<PaymentWithUserEmailDto>> ProcessFullPayment(CreatePaymentRequest request, Guid apartmentId, string idempotencyKey, Guid? userId = null, Guid? reservationId = null)
        {
            Reservation reservation;
            Guid userIdToUse;
            decimal amount;
            ReservationState? prevState = null;

            if (reservationId.HasValue)
            {
                // buscar la reserva y validarla 
                var existing = await GetPayableReservation(reservationId.Value, userId, ReservationState.NotConfirmed);
                if (!existing.Success) return Result<PaymentWithUserEmailDto>.Fail(existing.Error!);

                reservation = existing.Value!;
                userIdToUse = userId!.Value;
                prevState = reservation.State;

                // se busca el precio con los datos de la reserva en vez de los del request
                var amountResult = await GetFullPrice(reservation.ApartmentId, reservation.CheckInDate, reservation.CheckOutDate);
                if (!amountResult.Success) return Result<PaymentWithUserEmailDto>.Fail(amountResult.Error!);
                amount = amountResult.Value;

                var conflicting = await _reservationService.DatesAreInvalid(
                    reservation.CheckOutDate, reservation.CheckInDate, reservation.ApartmentId, reservation.Id);
                if (conflicting) return Result<PaymentWithUserEmailDto>.Fail("Ya hay una reserva con estas fechas");

                reservation.State = ReservationState.PaymentPending; // bloquea las fechas mientras se cobra
            }
            else
            {
                if (request.CheckOutDate.Date < request.CheckInDate.Date)
                    return Result<PaymentWithUserEmailDto>.Fail("La fecha de checkout no puede ser anterior a la de checkin");

                var amountResult = await GetFullPrice(apartmentId, request.CheckInDate, request.CheckOutDate);
                if (!amountResult.Success) return Result<PaymentWithUserEmailDto>.Fail(amountResult.Error!);
                amount = amountResult.Value;

                userIdToUse = userId ?? _userService.CreateUser(email: request.Payer.Email).Id;

                var reservationResult = await _reservationService.CreateReservation(request.CheckInDate, request.CheckOutDate, apartmentId, userIdToUse);
                if (!reservationResult.Success) return Result<PaymentWithUserEmailDto>.Fail(reservationResult.Error!);
                reservation = reservationResult.Value!;
            }

            await _db.SaveChangesAsync(); // bloquea las fechas antes de cobrar

            var paymentResult = await _bankService.CreatePayment(request, amount, userIdToUse, reservation.Id, idempotencyKey, isSign: false);
            if (!paymentResult.Success)
            {
                reservation.State = prevState ?? ReservationState.Cancelled;
                // borramos el usuario si fue un usuario ghost
                if (userId == null) await _userService.DeleteUser(userIdToUse);

                await _db.SaveChangesAsync();
                return Result<PaymentWithUserEmailDto>.Fail(paymentResult.Error!);
            }

            var payment = paymentResult.Value!;
            ApplyPaymentOutcome(reservation, payment, ReservationState.ConfirmedPaymentComplete, prevState);
            try { await _db.SaveChangesAsync(); }
            catch (DbUpdateException ex) when (ex.IsOverlap())
            {
                _db.ChangeTracker.Clear();
                // TODO: notificar al admin que tiene que devolver la plata
                return Result<PaymentWithUserEmailDto>.Fail("Ya hay una reserva con estas fechas. El administrador lo estara reembolsando en breve.");
            }

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
            Reservation reservation;
            decimal amount;
            ReservationState? prevState = null;
            if (reservationId.HasValue)
            {
                var existingReservation = await GetPayableReservation(reservationId.Value, userId, ReservationState.NotConfirmed);
                if (!existingReservation.Success) return Result<PaymentWithUserEmailDto>.Fail(existingReservation.Error!);
                reservation = existingReservation.Value!;
                prevState = reservation.State;

                // se consigue el monto a pagar con los datos de la reserva de la db en vez de los datos entrantes
                var amountResult = await GetSignPrice(reservation.ApartmentId, reservation.CheckInDate, reservation.CheckOutDate); // GetSignPrice
                if (!amountResult.Success) return Result<PaymentWithUserEmailDto>.Fail(amountResult.Error!);
                amount = amountResult.Value;

                // check for conflicting dates
                var conflicting = await _reservationService.DatesAreInvalid(reservation.CheckOutDate, reservation.CheckInDate, reservation.ApartmentId, reservation.Id);
                if (conflicting)
                    return Result<PaymentWithUserEmailDto>.Fail("Ya hay una reserva con estas fechas");

                reservation.State = ReservationState.PaymentPending; // lock down the reservation dates during payment so nobody else takes them.
            }
            else
            {
                if (request.CheckOutDate.Date < request.CheckInDate.Date)
                    return Result<PaymentWithUserEmailDto>.Fail("La fecha de checkout no puede ser anterior a la de checkin");

                var amountResult = await GetSignPrice(apartmentId, request.CheckInDate, request.CheckOutDate);
                if (!amountResult.Success) return Result<PaymentWithUserEmailDto>.Fail(amountResult.Error!);
                amount = amountResult.Value;

                var reservationResult = await _reservationService.CreateReservation(request.CheckInDate, request.CheckOutDate, apartmentId, userId);
                if (!reservationResult.Success) return Result<PaymentWithUserEmailDto>.Fail(reservationResult.Error!);
                reservation = reservationResult.Value!;
            }

            await _db.SaveChangesAsync(); // guardar los datos en la base de datos para que nadie entre con los mismos datos antes

            var paymentResult = await _bankService.CreatePayment(request, amount, userId, reservation.Id, idempotencyKey, isSign: true);
            if (!paymentResult.Success)
            {
                reservation.State = prevState ?? ReservationState.Cancelled; // se setea a cancelada en el caso de que es una reserva nueva
                await _db.SaveChangesAsync();
                return Result<PaymentWithUserEmailDto>.Fail(paymentResult.Error!);
            }


            var payment = paymentResult.Value!;
            ApplyPaymentOutcome(reservation, payment, ReservationState.ConfirmedPaymentIncomplete, prevState); // Incomplete al aprobar
            try { await _db.SaveChangesAsync(); }
            catch (DbUpdateException ex) when (ex.IsOverlap())
            {
                _db.ChangeTracker.Clear();
                // TODO: notificar al admin que tiene que devolver la plata
                return Result<PaymentWithUserEmailDto>.Fail("Ya hay una reserva con estas fechas. El administrador lo estara reembolsando en breve.");
            }

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
            // reservation already exists
            var existingReservation = await GetPayableReservation(reservationId, userId, ReservationState.ConfirmedPaymentIncomplete);
            if (!existingReservation.Success) return Result<PaymentWithUserEmailDto>.Fail("La reserva no existe");
            var reservation = existingReservation.Value!;
            var prevState = reservation.State;

            // conseguir monto
            var amountResult = await GetRestPrice(reservationId);
            if (!amountResult.Success) return Result<PaymentWithUserEmailDto>.Fail(amountResult.Error!);

            reservation.State = ReservationState.PaymentPending; // evita un segundo cobro mientras este esta en curso
            await _db.SaveChangesAsync();

            var paymentResult = await _bankService.CreatePayment(request, amountResult.Value, userId, reservation.Id, idempotencyKey, isSign: false);
            if (!paymentResult.Success)
            {
                reservation.State = prevState;
                await _db.SaveChangesAsync();
                return Result<PaymentWithUserEmailDto>.Fail(paymentResult.Error!);
            }

            Payment payment = paymentResult.Value!;
            ApplyPaymentOutcome(reservation, payment, ReservationState.ConfirmedPaymentComplete, prevState);
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
