//using MercadoPago.Resource.Payment;
using MercadoPago.Resource.User;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using SRM.Api.Data;
using SRM.Api.Models.Dto.Apartment;
using SRM.Api.Models.Dto.Payment;
using SRM.Api.Models.Dto.Reservation;
using SRM.Api.Models.Entities;
using SRM.Api.Models.Enums;
using SRM.Api.Services.Interfaces;
using SRM.Api.Utils;
using System;
using System.Collections.Generic;
using System.Text;



namespace SRM.Api.Services
{
    public class ReservationService(AppDbContext _db) : IReservationService
    {
        public async Task<Result<ReservationDetailDto>> GetReservationById(Guid reservationId) {
            var reservation = await _db.Reservations
                .AsNoTracking()
                .Where(r => r.Id == reservationId)
                .Select(r => new ReservationDetailDto {
                    ResrevationId = r.Id,
                    CheckInDate = r.CheckInDate,
                    CheckOutDate = r.CheckOutDate,
                    ApartmentId = r.ApartmentId,
                    State = r.State,
                    CreatedAt = r.CreatedAt,
                    UpdatedAt = r.UpdatedAt,
                    Payments = r.Payments.Select(p => p.ToDto()).ToList()

                })
                .FirstOrDefaultAsync();

            if (reservation == null)
                return Result<ReservationDetailDto>.Fail("No existe la reserva", 404);

            return Result<ReservationDetailDto>.Ok(reservation);

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
                    ReservationState = r.State
                })
                .ToListAsync();
            return Result<List<ReservationListingDto>>.Ok(reservations);
        }

        public async Task<bool> DatesAreInvalid(DateTime checkOutDate, DateTime checkInDate, Guid apartmentId)
        {
            var effectiveCheckOut = checkOutDate == checkInDate ? checkOutDate.AddDays(1) : checkOutDate;

            // Fetch conflicting reservations instead of just checking existence
            var conflictingReservations = await _db.Reservations
                .Where(r =>
                    (r.State == ReservationState.ConfirmedPaymentComplete ||
                     r.State == ReservationState.ConfirmedPaymentIncomplete ||
                     r.State == ReservationState.PaymentPending) &&
                    r.ApartmentId == apartmentId &&
                    r.CheckInDate < effectiveCheckOut &&
                    (r.CheckOutDate == r.CheckInDate ? r.CheckOutDate.AddDays(1) : r.CheckOutDate) > checkInDate
                )
                .ToListAsync();

            // Print details of conflicting records
            Console.WriteLine($"\n--- [DEBUG] DatesAreInvalid Check ---");
            Console.WriteLine($"Input CheckIn: {checkInDate} | CheckOut: {checkOutDate} | Apartment: {apartmentId}");
            Console.WriteLine($"Found {conflictingReservations.Count} conflicting reservation(s):");

            foreach (var r in conflictingReservations)
            {
                Console.WriteLine($" -> Reservation Id: {r.Id} | State: {r.State} | DB CheckIn: {r.CheckInDate} | DB CheckOut: {r.CheckOutDate}");
            }
            Console.WriteLine("-------------------------------------\n");

            return conflictingReservations.Any();
        }

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
        public async Task SaveChanges()
        {
            await _db.SaveChangesAsync();
        }
    }
}

