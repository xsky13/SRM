using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using SRM.Api.Data;
using SRM.Api.Models.Dto.Payment;
using SRM.Api.Models.Dto.Reservation;
using SRM.Api.Models.Entities;
using SRM.Api.Models.Enums;
using SRM.Api.Services;
using SRM.Api.Services.Interfaces;
using SRM.Api.Utils;

namespace SRM.UnitTests;

public class ReservationServiceTests
{
    static ReservationServiceTests()
    {
        Environment.SetEnvironmentVariable("SITE_URL", "http://test.local");
    }

    [Fact]
    public async Task EditOwnReservationFull_WithExtraCost_CreatesPendingPayment()
    {
        using var db = CreateDbContext();
        var user = CreateUser();
        var apartment = CreateApartment(100m);
        var reservation = new Reservation
        {
            Id = Guid.NewGuid(),
            ApartmentId = apartment.Id,
            Apartment = apartment,
            AppUserId = user.Id,
            AppUser = user,
            CheckInDate = new DateTime(2026, 1, 1),
            CheckOutDate = new DateTime(2026, 1, 3),
            State = ReservationState.ConfirmedPaymentIncomplete,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var approvedPayment = new Payment
        {
            Id = Guid.NewGuid(),
            Amount = 300m,
            PaymentStatus = PaymentStatus.Approved,
            ReservationId = reservation.Id,
            Reservation = reservation,
            AppUserId = user.Id,
            AppUser = user,
            PaymentDate = DateTime.UtcNow
        };
        db.AppUsers.Add(user);
        db.Apartments.Add(apartment);
        db.Reservations.Add(reservation);
        db.Payments.Add(approvedPayment);
        await db.SaveChangesAsync();

        var pendingPayment = new Payment
        {
            Id = Guid.NewGuid(),
            Amount = 200m,
            PaymentStatus = PaymentStatus.Pending,
            ReservationId = reservation.Id,
            AppUserId = user.Id,
            PaymentDate = DateTime.UtcNow
        };
        var bankService = new Mock<IBankService>();
        bankService
            .Setup(x => x.CreatePayment(
                It.IsAny<CreatePaymentRequest>(),
                200m,
                user.Id,
                reservation.Id,
                It.IsAny<string>(),
                false))
            .ReturnsAsync(Result<Payment>.Ok(pendingPayment));
        db.Payments.Add(pendingPayment);

        var service = new ReservationService(db, bankService.Object, Mock.Of<IMailService>());
        var newCheckIn = new DateTime(2026, 1, 1);
        var newCheckOut = new DateTime(2026, 1, 5);
        var result = await service.EditOwnReservationFull(new EditOwnReservationRequestPaymentDto
        {
            NewStartDate = newCheckIn,
            NewEndDate = newCheckOut,
            Token = "token",
            Installments = 1,
            Payment_Method_Id = "card",
            Payer = new Payer { Email = user.Email }
        }, reservation, user.Id);

        result.Success.Should().BeTrue();
        var savedReservation = await db.Reservations.FindAsync(reservation.Id);
        var savedPayment = await db.Payments.FindAsync(pendingPayment.Id);

        savedReservation!.State.Should().Be(ReservationState.PaymentPending);
        savedPayment.Should().NotBeNull();
        savedPayment!.PaymentStatus.Should().Be(PaymentStatus.Pending);
        savedPayment.PendingCheckInDate.Should().Be(newCheckIn);
        savedPayment.PendingCheckOutDate.Should().Be(newCheckOut);
        savedPayment.PreviousReservationState.Should().Be(ReservationState.ConfirmedPaymentIncomplete);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static AppUser CreateUser() => new()
    {
        Id = Guid.NewGuid(),
        Name = "Test",
        LastName = "User",
        Email = "test@test.com",
        Telefono = "123",
        Usertype = UserType.User
    };

    private static Apartment CreateApartment(decimal price) => new()
    {
        Id = Guid.NewGuid(),
        Name = "Apartment",
        Price = price,
        Location = "Address"
    };
}