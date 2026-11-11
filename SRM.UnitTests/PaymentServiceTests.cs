using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SRM.Api;
using SRM.Api.Data;
using SRM.Api.Models.Dto.Payment;
using SRM.Api.Models.Entities;
using SRM.Api.Models.Enums;
using SRM.Api.Services;
using SRM.Api.Services.Interfaces;
using SRM.Api.Utils;

namespace SRM.UnitTests;

public class PaymentServiceTests
{
    static PaymentServiceTests()
    {
        Environment.SetEnvironmentVariable("SITE_URL", "http://test.local");
    }

    [Fact]
    public async Task CalculateSignPrice_Returns10Percent()
    {
        using var db = CreateDbContext();
        var apartment = CreateApartment(1000m);
        db.Apartments.Add(apartment);
        await db.SaveChangesAsync();
        var paymentService = CreateService(db, new Mock<IBankService>().Object);

        var result = await paymentService.GetSignPrice(
            apartment.Id,
            new DateTime(2026, 1, 1),
            new DateTime(2026, 1, 3));

        result.Success.Should().BeTrue();
        result.Value.Should().Be(300m);
    }

    [Fact]
    public async Task ProcessSignPayment_Approved_UpdatesStateToIncomplete()
    {
        using var db = CreateDbContext();
        var user = CreateUser();
        var apartment = CreateApartment(1000m);
        var reservation = CreateReservation(user, apartment, ReservationState.NotConfirmed);
        var payment = CreatePayment(reservation, user, isSign: true, PaymentStatus.Approved, 300m);
        AddReservationGraph(db, user, apartment, reservation, payment);

        var bankService = new Mock<IBankService>();
        bankService
            .Setup(x => x.CreatePayment(It.IsAny<CreatePaymentRequest>(), It.IsAny<decimal>(), user.Id, reservation.Id, It.IsAny<string>(), true))
            .ReturnsAsync(Result<Payment>.Ok(payment));
        var paymentService = CreateService(db, bankService.Object);

        var result = await paymentService.ProcessSignPayment(
            CreateRequest(reservation), apartment.Id, "sign-key", user.Id, reservation.Id);

        result.Success.Should().BeTrue();
        var savedReservation = await db.Reservations.FindAsync(reservation.Id);
        var savedPayment = await db.Payments.FindAsync(payment.Id);
        savedReservation!.State.Should().Be(ReservationState.ConfirmedPaymentIncomplete);
        savedPayment!.IsSign.Should().BeTrue();
    }

    [Fact]
    public async Task ProcessFullPayment_Approved_UpdatesStateToComplete()
    {
        using var db = CreateDbContext();
        var user = CreateUser();
        var apartment = CreateApartment(1000m);
        var reservation = CreateReservation(user, apartment, ReservationState.NotConfirmed);
        var payment = CreatePayment(reservation, user, isSign: false, PaymentStatus.Approved, 3000m);
        AddReservationGraph(db, user, apartment, reservation, payment);

        var bankService = new Mock<IBankService>();
        bankService
            .Setup(x => x.CreatePayment(It.IsAny<CreatePaymentRequest>(), It.IsAny<decimal>(), user.Id, reservation.Id, It.IsAny<string>(), false))
            .ReturnsAsync(Result<Payment>.Ok(payment));
        var paymentService = CreateService(db, bankService.Object);

        var result = await paymentService.ProcessFullPayment(
            CreateRequest(reservation), apartment.Id, "full-key", user.Id, reservation.Id);

        result.Success.Should().BeTrue();
        var savedReservation = await db.Reservations.FindAsync(reservation.Id);
        var savedPayment = await db.Payments.FindAsync(payment.Id);
        savedReservation!.State.Should().Be(ReservationState.ConfirmedPaymentComplete);
        savedPayment!.IsSign.Should().BeFalse();
    }

    [Fact]
    public async Task ProcessPayment_Rejected_RollsBackState()
    {
        using var db = CreateDbContext();
        var user = CreateUser();
        var apartment = CreateApartment(1000m);
        var reservation = CreateReservation(user, apartment, ReservationState.ConfirmedPaymentIncomplete);
        var payment = CreatePayment(reservation, user, isSign: false, PaymentStatus.Rejected, 2000m);
        AddReservationGraph(db, user, apartment, reservation, payment);

        var bankService = new Mock<IBankService>();
        bankService
            .Setup(x => x.CreatePayment(It.IsAny<CreatePaymentRequest>(), It.IsAny<decimal>(), user.Id, reservation.Id, It.IsAny<string>(), false))
            .ReturnsAsync(Result<Payment>.Ok(payment));
        var paymentService = CreateService(db, bankService.Object);

        var result = await paymentService.ProcessRestPayment(
            CreateRequest(reservation), reservation.Id, "rejected-key", user.Id);

        result.Success.Should().BeTrue();
        var savedReservation = await db.Reservations.FindAsync(reservation.Id);
        savedReservation!.State.Should().Be(ReservationState.ConfirmedPaymentIncomplete);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static PaymentService CreateService(AppDbContext db, IBankService bankService)
    {
        var reservationService = new Mock<IReservationService>();
        reservationService
            .Setup(x => x.DatesAreInvalid(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<Guid>(), It.IsAny<Guid?>()))
            .ReturnsAsync(false);
        return new PaymentService(
            db,
            reservationService.Object,
            bankService,
            Mock.Of<IUserService>(),
            NullLogger<GlobalExceptionHandler>.Instance,
            Mock.Of<IMailService>());
    }

    private static void AddReservationGraph(
        AppDbContext db,
        AppUser user,
        Apartment apartment,
        Reservation reservation,
        Payment payment)
    {
        db.AppUsers.Add(user);
        db.Apartments.Add(apartment);
        db.Reservations.Add(reservation);
        db.Payments.Add(payment);
        db.SaveChanges();
    }

    private static CreatePaymentRequest CreateRequest(Reservation reservation) => new()
    {
        CheckInDate = reservation.CheckInDate,
        CheckOutDate = reservation.CheckOutDate,
        TransactionAmount = 1000m,
        Token = "token",
        Installments = 1,
        Payment_Method_Id = "card",
        Payer = new Payer { Email = reservation.AppUser.Email }
    };

    private static Payment CreatePayment(
        Reservation reservation,
        AppUser user,
        bool isSign,
        PaymentStatus status,
        decimal amount) => new()
        {
            Id = Guid.NewGuid(),
            Amount = amount,
            IsSign = isSign,
            PaymentStatus = status,
            PaymentDate = DateTime.UtcNow,
            ReservationId = reservation.Id,
            Reservation = reservation,
            AppUserId = user.Id,
            AppUser = user,
            PreviousReservationState = reservation.State
        };

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

    private static Reservation CreateReservation(AppUser user, Apartment apartment, ReservationState state) => new()
    {
        Id = Guid.NewGuid(),
        ApartmentId = apartment.Id,
        Apartment = apartment,
        AppUserId = user.Id,
        AppUser = user,
        CheckInDate = new DateTime(2026, 1, 1),
        CheckOutDate = new DateTime(2026, 1, 3),
        State = state,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };
}
