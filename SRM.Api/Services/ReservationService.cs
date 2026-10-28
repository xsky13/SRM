//using MercadoPago.Resource.Payment;
using MercadoPago.Resource.Payment;
using Microsoft.EntityFrameworkCore;
using SRM.Api.Data;
using SRM.Api.Models.Dto.Apartment;
using SRM.Api.Models.Dto.Payment;
using SRM.Api.Models.Dto.Reservation;
using SRM.Api.Models.Entities;
using SRM.Api.Models.Enums;
using SRM.Api.Services.Interfaces;
using SRM.Api.Utils;


namespace SRM.Api.Services
{
    public class ReservationService(AppDbContext _db, IBankService _bankService) : IReservationService
    {
        public async Task<Result<ReservationDetailDto>> GetReservationById(Guid reservationId, Guid userId)
        {
            ReservationDetailDto returnedReservation;
            var reservation = await _db.Reservations
                .AsNoTracking()
                .Where(r => r.Id == reservationId)
                .Select(r => new ReservationDetailDto
                {
                    ResrevationId = r.Id,
                    CheckInDate = r.CheckInDate,
                    CheckOutDate = r.CheckOutDate,
                    ApartmentId = r.ApartmentId,
                    State = r.State,
                    CreatedAt = r.CreatedAt,
                    UpdatedAt = r.UpdatedAt,
                    AppUserId = r.AppUserId,
                    Payments = r.Payments.Select(p => p.ToDto()).ToList(),
                    Apartment = new ApartmentListingDto(r.ApartmentId, r.Apartment.Name, r.Apartment.CoverImgUrl, r.Apartment.Location, r.Apartment.Price)
                })
                .FirstOrDefaultAsync();

            if (reservation == null)
                return Result<ReservationDetailDto>.Fail("No existe la reserva", 404);

            if (reservation.AppUserId != userId)
                return Result<ReservationDetailDto>.Fail("No existe la reserva", 403);


            returnedReservation = reservation;

            decimal apartmentCost = await _db.Apartments
                .Where(a => a.Id == reservation.ApartmentId)
                .Select(a => a.Price)
                .FirstOrDefaultAsync();

            if (apartmentCost == 0) // no se encontro el departamento
                return Result<ReservationDetailDto>.Fail("El departamento no existe.");

            var diff = (reservation.CheckOutDate.Date - reservation.CheckInDate.Date).Days + 1;
            var nights = Math.Max(diff, 1);
            var fullCost = nights * apartmentCost;

            returnedReservation.FullCost = fullCost;


            return Result<ReservationDetailDto>.Ok(returnedReservation);

        }

        public async Task<Result<List<ReservationListingDto>>> GetAllByApartmentId(Guid id)
        {
            // Start of today in UTC (00:00:00)
            var today = DateTime.UtcNow.Date;
            var maxDate = today.AddMonths(2);

            var reservations = await _db.Reservations
                .AsNoTracking()
                .Where(r => r.ApartmentId == id
                    && (r.State == ReservationState.ConfirmedPaymentComplete
                        || r.State == ReservationState.ConfirmedPaymentIncomplete
                        || r.State == ReservationState.PaymentPending)
                    // Includes today's check-ins AND ongoing stays checking out today or later
                    && r.CheckOutDate >= today
                    && r.CheckInDate <= maxDate)
                .Select(r => new ReservationListingDto
                {
                    Id = r.Id,
                    CheckInDate = r.CheckInDate,
                    CheckOutDate = r.CheckOutDate,
                    ApartmentId = r.ApartmentId,
                    ReservationState = r.State
                })
                .ToListAsync();

            return Result<List<ReservationListingDto>>.Ok(reservations);
        }

        public async Task<Result<List<ReservationListingDto>>> GetByUserId(Guid userId)
        {
            var reservations = await _db.Reservations
                .AsNoTracking()
                .Where(r => r.AppUserId == userId)
                .Select(r => new ReservationListingDto
                {
                    Id = r.Id,
                    CheckInDate = r.CheckInDate,
                    CheckOutDate = r.CheckOutDate,
                    ApartmentId = r.ApartmentId,
                    ReservationState = r.State,
                    ApartmentName = r.Apartment.Name
                })
                .ToListAsync();
            return Result<List<ReservationListingDto>>.Ok(reservations);
        }

        public async Task<bool> DatesAreInvalid(DateTime checkOutDate, DateTime checkInDate, Guid apartmentId, Guid? excludeReservationId = null)
        {
            /*
                excludeReservationId
                Es para el momento de editar reserva propia. Si queremos buscar reservas ya confirmadas en las nuevas fechas seleccionadas, hay que exlcuir 
                la reserva actual.
             */
            var checkIn = checkInDate.Date;
            var checkOut = checkOutDate.Date;
            var effectiveCheckOut = checkOut == checkIn ? checkOut.AddDays(1) : checkOut;

            return await _db.Reservations.AnyAsync(r =>
                r.ApartmentId == apartmentId &&
                (excludeReservationId == null || r.Id != excludeReservationId) &&
                (r.State == ReservationState.ConfirmedPaymentComplete ||
                 r.State == ReservationState.ConfirmedPaymentIncomplete ||
                 r.State == ReservationState.PaymentPending) &&
                r.CheckInDate < effectiveCheckOut &&
                (r.CheckOutDate == r.CheckInDate ? r.CheckOutDate.AddDays(1) : r.CheckOutDate) > checkIn
            );
        }

        public async Task<Result<Reservation>> CreateReservation(DateTime checkInDate, DateTime checkOutDate, Guid apartmentId, Guid userId)
        {
            var datesInvalid = await DatesAreInvalid(checkOutDate, checkInDate, apartmentId);
            if (datesInvalid) return Result<Reservation>.Fail("Fechas invalidas");


            var reservation = new Reservation
            {
                Id = Guid.NewGuid(),
                CheckInDate = checkInDate,
                CheckOutDate = checkOutDate,
                State = ReservationState.PaymentPending,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                ApartmentId = apartmentId,
                AppUserId = userId
            };

            _db.Reservations.Add(reservation);

            try { await _db.SaveChangesAsync(); }
            catch (DbUpdateException ex) when (ex.IsOverlap())
            {
                _db.ChangeTracker.Clear();
                return Result<Reservation>.Fail("Ya hay una reserva con estas fechas");
            }

            return Result<Reservation>.Ok(reservation);
        }

        public async Task<Result<Reservation>> CreateAuthReservation(DateTime checkInDate, DateTime checkOutDate, Guid apartmentId, Guid userId)
        {
            var datesInvalid = await DatesAreInvalid(checkOutDate, checkInDate, apartmentId);
            if (datesInvalid) return Result<Reservation>.Fail("Fechas invalidas");


            var reservation = new Reservation
            {
                Id = Guid.NewGuid(),
                CheckInDate = checkInDate,
                CheckOutDate = checkOutDate,
                State = ReservationState.NotConfirmed,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                ApartmentId = apartmentId,
                AppUserId = userId
            };

            _db.Reservations.Add(reservation);
            await _db.SaveChangesAsync();

            return Result<Reservation>.Ok(reservation);
        }

        public async Task<Result<Reservation>> AdminCreateReservationAsync(AdminCreateReservationDto request)
        {
            var apartmentExists = await _db.Apartments.AnyAsync(a => a.Id == request.ApartmentId);
            if (!apartmentExists)
                return Result<Reservation>.Fail("El departamento no existe.", 404);

            if (request.EndDate.Date < request.StartDate.Date)
                return Result<Reservation>.Fail("La fecha de salida no puede ser anterior a la de entrada.");

            var datesInvalid = await DatesAreInvalid(request.EndDate, request.StartDate, request.ApartmentId);
            if (datesInvalid)
                return Result<Reservation>.Fail("Ya hay una reserva con estas fechas.");

            Guid userId;

            if (request.IsRegisteredUser)
            {
                if (request.UserId == null)
                    return Result<Reservation>.Fail("El usuario registrado es obligatorio.");

                var registeredUserExists = await _db.AppUsers.AnyAsync(u => u.Id == request.UserId.Value);
                if (!registeredUserExists)
                    return Result<Reservation>.Fail("El usuario no existe.", 404);

                userId = request.UserId.Value;
            }
            else
            {
                if (request.GuestUserInfo == null)
                    return Result<Reservation>.Fail("La información del usuario Guest es obligatoria.");

                var guestInfo = request.GuestUserInfo;
                var emailExists = await _db.AppUsers.AnyAsync(u => u.Email == guestInfo.Email);
                if (emailExists)
                    return Result<Reservation>.Fail("Ya existe un usuario con ese email.");

                var guestUser = new AppUser
                {
                    Id = Guid.NewGuid(),
                    Name = guestInfo.FirstName,
                    LastName = guestInfo.LastName,
                    Telefono = guestInfo.Telefono,
                    Email = guestInfo.Email,
                    PwdHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString()),
                    Usertype = UserType.Guest
                };

                _db.AppUsers.Add(guestUser);
                userId = guestUser.Id;
            }

            var reservation = new Reservation
            {
                Id = Guid.NewGuid(),
                CheckInDate = request.StartDate,
                CheckOutDate = request.EndDate,
                State = ReservationState.ConfirmedPaymentIncomplete,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                ApartmentId = request.ApartmentId,
                AppUserId = userId
            };

            _db.Reservations.Add(reservation);

            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (ex.IsOverlap())
            {
                _db.ChangeTracker.Clear();
                return Result<Reservation>.Fail("Ya hay una reserva con estas fechas.");
            }

            return Result<Reservation>.Ok(reservation);
        }


        public async Task SaveChanges()
        {
            await _db.SaveChangesAsync();
        }


        public async Task<Result<Reservation>> ValidateReservation(DateTime newEndDate, DateTime newStartDate, Guid reservationId, Guid userId)
        {
            var reservation = await _db.Reservations
                .Include(r => r.Apartment)
                .FirstOrDefaultAsync(r => r.Id == reservationId);
            if (reservation == null) return Result<Reservation>.Fail("La reserva no existe", 404);

            if (newEndDate.Date < newStartDate.Date)
                return Result<Reservation>.Fail("La fecha de salida no puede ser anterior a la de entrada.");

            if (newStartDate.Date < DateTime.UtcNow.Date)
                return Result<Reservation>.Fail("La nueva fecha de entrada no puede estar en el pasado.");

            if (reservation.AppUserId != userId) return Result<Reservation>.Fail("Esta reserva no le pertenece.", 403);

            if (reservation.State == ReservationState.Cancelled || reservation.State == ReservationState.PaymentPending)
                return Result<Reservation>.Fail("Esta reserva no se puede modificar. Hay un pago pendiente o esta cancelada.");

            // TODO: validar?
            // la reserva solo se puede modificar hasta 24 horas antes de que entre?
            var now = DateTime.UtcNow;
            var timePassed = reservation.CheckInDate - now;
            if (timePassed.TotalHours < 24) return Result<Reservation>.Fail("No puede modificar su reserva cuando faltan menos de 24 horas para que comienze.");

            // checkear fechas
            var datesInvalid = await DatesAreInvalid(newEndDate, newStartDate, reservation.ApartmentId, reservation.Id);
            if (datesInvalid) return Result<Reservation>.Fail("Fechas invalidas.");

            return Result<Reservation>.Ok(reservation);
        }


        public async Task<Result<ReservationDetailDto>> EditOwnReservation(EditOwnReservationRequestNoPaymentDto request, Reservation reservation, Guid userId)
        {
            reservation.CheckInDate = request.NewStartDate;
            reservation.CheckOutDate = request.NewEndDate;
            reservation.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            if (reservation.State == ReservationState.ConfirmedPaymentComplete)
            {
                // notificar al administrador y usuario
            }

            return Result<ReservationDetailDto>.Ok(reservation.ToReservationDetailDto());
        }

        public async Task<Result<ReservationDetailDto>> EditOwnReservationFull(EditOwnReservationRequestPaymentDto request, Reservation reservation, Guid userId)
        {
            // esta confirmado por el controlador que el estado de la reserva es o confirmedpaymentincomplete o confirmedpaymentcomplete, y que las fechas de la reserva son validas
            var totalPayed = await _db.Payments
                .Where(p => p.ReservationId == reservation.Id && p.PaymentStatus == Models.Enums.PaymentStatus.Approved)
                .SumAsync(p => p.Amount);
            

            /*
             Conseguir nuevo costo total
             */
            decimal apartmentCost = await _db.Apartments
                .Where(a => a.Id == reservation.ApartmentId)
                .Select(a => a.Price)
                .FirstOrDefaultAsync();

            if (apartmentCost == 0) // no se encontro el departamento
                return Result<ReservationDetailDto>.Fail("El departamento para los pagos no existe.");

            var nights = (request.NewEndDate.Date - request.NewStartDate.Date).Days + 1;
            var fullCost = nights * apartmentCost;
            var amount = fullCost - totalPayed; // diferencia entre lo pagado y el nuevo total

            if (amount <= 0)
                return Result<ReservationDetailDto>.Fail("No hay nada para cobrar con estas fechas.");

            /*
                Pagar
             */
            CreatePaymentRequest paymentRequest = new()
            {
                TransactionAmount = amount,
                Token = request.Token,
                Installments = request.Installments,
                Payment_Method_Id = request.Payment_Method_Id,
                Payer = request.Payer,
                ReservationId = reservation.Id,
                CheckInDate = request.NewStartDate,
                CheckOutDate = request.NewEndDate
            };
            var paymentResult = await _bankService.CreatePayment(paymentRequest, amount, userId, reservation.Id, "", isSign: false); // TODO: resolver idempotency (later)
            if (!paymentResult.Success) return Result<ReservationDetailDto>.Fail(paymentResult.Error!);
            var payment = paymentResult.Value!;

            switch (payment.PaymentStatus)
            {
                case Models.Enums.PaymentStatus.Approved:
                    reservation.CheckInDate = request.NewStartDate;
                    reservation.CheckOutDate = request.NewEndDate;
                    reservation.State = ReservationState.ConfirmedPaymentComplete;
                    reservation.UpdatedAt = DateTime.UtcNow;
                    await _db.SaveChangesAsync();
                    return Result<ReservationDetailDto>.Ok(reservation.ToReservationDetailDto());

                case Models.Enums.PaymentStatus.Rejected or Models.Enums.PaymentStatus.Cancelled:
                    await _db.SaveChangesAsync(); // registra el intento fallido
                    return Result<ReservationDetailDto>.Fail("El pago fue rechazado. Sus fechas no fueron modificadas.");

                default: // Pending, in_process, authorized
                    payment.PendingCheckInDate = request.NewStartDate;
                    payment.PendingCheckOutDate = request.NewEndDate;
                    payment.PreviousReservationState = reservation.State;
                    reservation.State = ReservationState.PaymentPending;
                    reservation.UpdatedAt = DateTime.UtcNow;
                    await _db.SaveChangesAsync();
                    // la reserva queda con las fechas viejas; el webhook las aplica si el pago se aprueba
                    return Result<ReservationDetailDto>.Ok(reservation.ToReservationDetailDto());
            }
        }
        

        public async Task<Result<ReservationDetailDto>> CancelReservation(Guid reservationId, Guid userId)
        {
            var user = await _db.AppUsers.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
                return Result<ReservationDetailDto>.Fail("Usuario no encontrado.", 401);

            var reservation = await _db.Reservations
                .Include(r => r.Payments)
                .FirstOrDefaultAsync(r => r.Id == reservationId);
            if (reservation == null)
                return Result<ReservationDetailDto>.Fail("No se encontró la reserva.", 404);

            // dueño o el que hizo la reserva puede cancelarla
            if (user.Usertype != UserType.Admin && reservation.AppUserId != userId)
                return Result<ReservationDetailDto>.Fail("No tenés permiso para cancelar esta reserva.", 403);

            if (reservation.State == ReservationState.Cancelled)
                return Result<ReservationDetailDto>.Fail("La reserva ya está cancelada.", 400);

            if (DateTime.UtcNow > reservation.CheckInDate.AddHours(-24))
                return Result<ReservationDetailDto>.Fail(
                    $"No se puede cancelar: faltan menos de 24 hs para el check-in ({reservation.CheckInDate:dd/MM/yyyy HH:mm}).",
                    400);

            var pagosAprobados = reservation.Payments
                .Where(p => p.PaymentStatus == Models.Enums.PaymentStatus.Approved)
                .ToList();

            var senaRetenida = pagosAprobados.Where(p => p.IsSign).Sum(p => p.Amount);
            var montoADevolver = pagosAprobados.Where(p => !p.IsSign).Sum(p => p.Amount);

            //    Ejemplo para notificacion:
            //    if (montoADevolver > 0)
            //        await _notificationService.NotifyAdminRefundAsync(reservationId, montoADevolver);

            // (soft delete)
            reservation.State = ReservationState.Cancelled;
            reservation.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return Result<ReservationDetailDto>.Ok(reservation.ToReservationDetailDto());
        }
    }
}

