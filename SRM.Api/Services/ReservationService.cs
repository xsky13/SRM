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
        public async Task<Result<ReservationDetailDto>> GetReservationById(Guid reservationId)
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
                    Payments = r.Payments.Select(p => p.ToDto()).ToList(),
                    Apartment = new ApartmentListingDto(r.ApartmentId, r.Apartment.Name, r.Apartment.CoverImgUrl, r.Apartment.Location, r.Apartment.Price)
                })
                .FirstOrDefaultAsync();


            if (reservation == null)
                return Result<ReservationDetailDto>.Fail("No existe la reserva", 404);


            returnedReservation = reservation;

            decimal apartmentCost = await _db.Apartments
                .Where(a => a.Id == reservation.ApartmentId)
                .Select(a => a.Price)
                .FirstOrDefaultAsync();

            if (apartmentCost == 0) // no se encontro el departamento
                return Result<ReservationDetailDto>.Fail("El departamento no existe.");

            var diff = (reservation.CheckOutDate.Date - reservation.CheckInDate.Date).Days;
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

        private static int Nights(DateTime checkIn, DateTime checkOut) =>  Math.Max((checkOut.Date - checkIn.Date).Days, 1);

        public async Task<Result<Reservation>> CreateReservation(DateTime checkInDate, DateTime checkOutDate, Guid apartmentId, Guid userId)
        {
            Console.WriteLine($"\n\n checkDate: {checkInDate} \n\n");
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


        public async Task SaveChanges()
        {
            await _db.SaveChangesAsync();
        }


        public async Task<decimal> UnpaidAmount(Guid reservationId, decimal newPrice)
        {
            var payments = await _db.Payments
                .Select(p => new { p.ReservationId, p.Amount })
                .Where(p => p.ReservationId == reservationId).ToListAsync();

            decimal totalPayed = 0;
            foreach (var payment in payments)
                totalPayed += payment.Amount;
            return newPrice - totalPayed;
        }


        public async Task<Result<ReservationDetailDto>> EditOwnReservation(EditOwnReservationRequestDto request, Guid reservationId, Guid userId)
        {
            if (request.NewEndDate.Date < request.NewStartDate.Date)
                return Result<ReservationDetailDto>.Fail("La fecha de salida no puede ser anterior a la de entrada.");

            var reservation = await _db.Reservations
                .Include(r => r.Apartment)
                .FirstOrDefaultAsync(r => r.Id == reservationId);

            if (reservation == null) return Result<ReservationDetailDto>.Fail("La reserva no existe", 404);

            if (reservation.AppUserId != userId) return Result<ReservationDetailDto>.Fail("Esta reserva no le pertenece.", 403);

            // TODO: validar?
            // la reserva solo se puede modificar hasta 24 horas antes de que entre?
            var now = DateTime.UtcNow;
            var timePassed = reservation.CheckInDate - now;
            if (timePassed.TotalHours < 24) return Result<ReservationDetailDto>.Fail("No puede modificar su reserva cuando faltan menos de 24 horas para que comienze.");

            // checkear fechas
            var datesInvalid = await DatesAreInvalid(request.NewEndDate, request.NewStartDate, reservation.ApartmentId, reservationId);
            if (datesInvalid) return Result<ReservationDetailDto>.Fail("Fechas invalidas.");

            var oldDays = Nights(reservation.CheckInDate, reservation.CheckOutDate);
            var newDays = Nights(request.NewStartDate, request.NewEndDate);
            var newPrice = reservation.Apartment.Price * newDays;
            var oldPrice = reservation.Apartment.Price * oldDays;

            if (newDays <= oldDays && reservation.State == ReservationState.ConfirmedPaymentComplete)
            {
                // ya fue hecho todo el pago, notificar al administrador que debe compensar al usuario con la diferencia
                var owed = oldPrice - newPrice;

                // mandar notificacion al admin con el precio que le debe 


                reservation.CheckInDate = request.NewStartDate;
                reservation.CheckOutDate = request.NewEndDate;
                reservation.State = ReservationState.ConfirmedPaymentComplete;
                await _db.SaveChangesAsync();

                return Result<ReservationDetailDto>.Ok(reservation.ToReservationDetailDto());
            }
            else if (newDays > oldDays && reservation.State != ReservationState.Cancelled && reservation.State != ReservationState.PaymentPending)
            {
                // el usuario selecciono mas tiempo, y la reserva esta no confirmada o solo con la sena pagada.
                // debe pagar el nuevo costo completo para continuar

                var amount = await (reservation.State switch
                {
                    ReservationState.NotConfirmed => Task.FromResult(newPrice),
                    ReservationState.ConfirmedPaymentIncomplete or ReservationState.ConfirmedPaymentComplete => UnpaidAmount(reservationId, newPrice),
                    _ => Task.FromResult(newPrice)
                });

                CreatePaymentRequest paymentRequest = new()
                {
                    TransactionAmount = amount,
                    Token = request.Token,
                    Installments = request.Installments,
                    Payment_Method_Id = request.Payment_Method_Id,
                    Payer = request.Payer,
                    ReservationId = request.ReservationId,
                    CheckInDate = request.NewStartDate,
                    CheckOutDate = request.NewEndDate
                };
                var paymentResult = await _bankService.CreatePayment(paymentRequest, amount, userId, reservationId, "", isSign: false); // TODO: resolver idempotency (later)
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
                        await _db.SaveChangesAsync();
                        // la reserva queda con las fechas viejas; el webhook las aplica si el pago se aprueba
                        return Result<ReservationDetailDto>.Ok(reservation.ToReservationDetailDto());
                }

                return Result<ReservationDetailDto>.Ok(reservation.ToReservationDetailDto());
            }
            else return Result<ReservationDetailDto>.Fail("Su reserva esta cancelada o con un pago pendiente. Por favor, espere un momento e intente de nuevo. Si el error persiste, contactese con un administrador.");
        }

    }
}

