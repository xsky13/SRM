using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using SRM.Api.Data;
using SRM.Api.Models.Dto.Apartment;
using SRM.Api.Models.Dto.Reservation;
using SRM.Api.Models.Dto.Payment;
using SRM.Api.Models.Entities;
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
                    Payments = MapPaymentsToDto(r.Payments),

                })
                .FirstOrDefaultAsync();

            return Result<ReservationDetailDto>.Ok(reservation);

        }

        private List<PaymentDto> MapPaymentsToDto(List<Payment> payments)
        {
            // esta funcion mapea la lista de pagos a una lista de PaymentDto
            if (payments == null || payments.Count == 0)
                return new List<PaymentDto>();

            return payments.Select(p => new PaymentDto(
                p.Id,
                p.Amount,
                p.IsManual,
                p.IsSign,
                p.PaymentDate,
                p.PaymentStatus,
                p.ReservationId,
                p.AppUserId,
                p.TicketId
            )).ToList();
        }

        public async Task<Result<List<ReservationListingDto>>> GetAllByApartmentId(Guid id)
        {
            // Fecha límite: hoy + 2 meses
            var maxDate = DateTime.Now.AddMonths(2);

            var reservations = await _db.Reservations
                .AsNoTracking()
                .Where(r => r.ApartmentId == id
                            && r.CheckInDate <= maxDate)  // Validación: no más de 2 meses hacia adelante
                .Select(r => new ReservationListingDto
                {
                    Id = r.Id,
                    CheckInDate = r.CheckInDate,
                    CheckOutDate = r.CheckOutDate,
                    ApartmentId = r.ApartmentId
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
                    ApartmentId = r.ApartmentId
                })
                .ToListAsync();
            return Result<List<ReservationListingDto>>.Ok(reservations);
        }
    }
}
