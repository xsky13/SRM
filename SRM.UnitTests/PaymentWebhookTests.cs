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

public class PaymentWebhookTests
{
    static PaymentWebhookTests()
    {
        Environment.SetEnvironmentVariable("SITE_URL", "http://test.local");
        Environment.SetEnvironmentVariable("ADMIN_MAIL", "admin@test.local");
    }

    [Fact]
    public async Task Webhook_ApprovedWithDateChange_Available_UpdatesDatesAndState()
    {
        using var db = CreateDbContext();
        var user = CreateUser();
        var apartment = CreateApartment(1000m);
        var reservation = CreateReservation(user, apartment, ReservationState.PaymentPending);
        var pendingCheckIn = new DateTime(2026, 2, 1);
        var pendingCheckOut = new DateTime(2026, 2, 5);
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            Amount = 2000m,
            PaymentStatus = PaymentStatus.Pending,
            MpPaymentId = "111111",
            ReservationId = reservation.Id,
            AppUserId = user.Id,
            PaymentDate = DateTime.UtcNow,
            PendingCheckInDate = pendingCheckIn,
            PendingCheckOutDate = pendingCheckOut,
            PreviousReservationState = ReservationState.ConfirmedPaymentIncomplete
        };

        db.AppUsers.Add(user);
        db.Apartments.Add(apartment);
        db.Reservations.Add(reservation);
        db.Payments.Add(payment);
        await db.SaveChangesAsync();

        var bankService = new Mock<IBankService>();
        bankService.Setup(x => x.GetAsync(It.IsAny<long>())).ReturnsAsync("approved");
        var reservationService = new Mock<IReservationService>();
        reservationService
            .Setup(x => x.DatesAreInvalid(pendingCheckOut, pendingCheckIn, apartment.Id, reservation.Id))
            .ReturnsAsync(false);

        var paymentService = CreateService(db, bankService.Object, reservationService.Object);

        await paymentService.CardPaymentWebhook(new PaymentWebhookRequest
        {
            Type = "payment",
            Data = new Data { Id = payment.MpPaymentId }
        });

        var savedReservation = await db.Reservations.FindAsync(reservation.Id);
        var savedPayment = await db.Payments.FindAsync(payment.Id);

        savedReservation!.CheckInDate.Should().Be(pendingCheckIn);
        savedReservation.CheckOutDate.Should().Be(pendingCheckOut);
        savedReservation.State.Should().Be(ReservationState.ConfirmedPaymentComplete);
        savedPayment!.PendingCheckInDate.Should().BeNull();
        savedPayment.PendingCheckOutDate.Should().BeNull();
        savedPayment.PreviousReservationState.Should().BeNull();
    }

    [Fact]
    public async Task Webhook_ApprovedWithDateChange_Unavailable_KeepsPreviousState()
    {
        using var db = CreateDbContext();
        var user = CreateUser();
        var apartment = CreateApartment(1000m);
        var reservation = CreateReservation(user, apartment, ReservationState.PaymentPending);
        var originalCheckIn = reservation.CheckInDate;
        var originalCheckOut = reservation.CheckOutDate;
        var pendingCheckIn = new DateTime(2026, 2, 1);
        var pendingCheckOut = new DateTime(2026, 2, 5);
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            Amount = 2000m,
            PaymentStatus = PaymentStatus.Pending,
            MpPaymentId = "222222",
            ReservationId = reservation.Id,
            AppUserId = user.Id,
            PaymentDate = DateTime.UtcNow,
            PendingCheckInDate = pendingCheckIn,
            PendingCheckOutDate = pendingCheckOut,
            PreviousReservationState = ReservationState.ConfirmedPaymentIncomplete
        };

        db.AppUsers.Add(user);
        db.Apartments.Add(apartment);
        db.Reservations.Add(reservation);
        db.Payments.Add(payment);
        await db.SaveChangesAsync();

        var bankService = new Mock<IBankService>();
        bankService.Setup(x => x.GetAsync(It.IsAny<long>())).ReturnsAsync("approved");
        var reservationService = new Mock<IReservationService>();
        reservationService
            .Setup(x => x.DatesAreInvalid(pendingCheckOut, pendingCheckIn, apartment.Id, reservation.Id))
            .ReturnsAsync(true);

        var paymentService = CreateService(db, bankService.Object, reservationService.Object);

        await paymentService.CardPaymentWebhook(new PaymentWebhookRequest
        {
            Type = "payment",
            Data = new Data { Id = payment.MpPaymentId }
        });

        var savedReservation = await db.Reservations.FindAsync(reservation.Id);

        savedReservation!.CheckInDate.Should().Be(originalCheckIn);
        savedReservation.CheckOutDate.Should().Be(originalCheckOut);
        savedReservation.State.Should().Be(ReservationState.ConfirmedPaymentIncomplete);
    }

    [Fact]
    public async Task Webhook_ApprovedSignPayment_TransitionsToIncomplete()
    {
        using var db = CreateDbContext();
        var user = CreateUser();
        var apartment = CreateApartment(1000m);
        var reservation = CreateReservation(user, apartment, ReservationState.PaymentPending);
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            Amount = 250m,
            IsSign = true,
            PaymentStatus = PaymentStatus.Pending,
            MpPaymentId = "123",
            ReservationId = reservation.Id,
            AppUserId = user.Id,
            PaymentDate = DateTime.UtcNow,
            PreviousReservationState = null
        };

        db.Apartments.Add(apartment);
        db.AppUsers.Add(user);
        db.Reservations.Add(reservation);
        db.Payments.Add(payment);
        await db.SaveChangesAsync();

        var bankService = new Mock<IBankService>();
        bankService.Setup(x => x.GetAsync(It.IsAny<long>())).ReturnsAsync("approved");

        var paymentService = CreateService(db, bankService.Object);

        await paymentService.CardPaymentWebhook(new PaymentWebhookRequest
        {
            Type = "payment",
            Data = new Data { Id = payment.MpPaymentId }
        });

        var savedReservation = await db.Reservations.FindAsync(reservation.Id);
        var savedPayment = await db.Payments.FindAsync(payment.Id);

        savedReservation!.State.Should().Be(ReservationState.ConfirmedPaymentIncomplete);
        savedPayment!.PaymentStatus.Should().Be(PaymentStatus.Approved);
    }

    [Fact]
    public async Task Webhook_ApprovedFullPayment_TransitionsToComplete()
    {
        using var db = CreateDbContext();
        var user = CreateUser();
        var apartment = CreateApartment(2000m);
        var reservation = CreateReservation(user, apartment, ReservationState.PaymentPending);
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            Amount = 2000m,
            IsSign = false,
            PaymentStatus = PaymentStatus.Pending,
            MpPaymentId = "456",
            ReservationId = reservation.Id,
            AppUserId = user.Id,
            PaymentDate = DateTime.UtcNow,
            PreviousReservationState = null
        };

        db.Apartments.Add(apartment);
        db.AppUsers.Add(user);
        db.Reservations.Add(reservation);
        db.Payments.Add(payment);
        await db.SaveChangesAsync();

        var bankService = new Mock<IBankService>();
        bankService.Setup(x => x.GetAsync(It.IsAny<long>())).ReturnsAsync("approved");

        var paymentService = CreateService(db, bankService.Object);

        await paymentService.CardPaymentWebhook(new PaymentWebhookRequest
        {
            Type = "payment",
            Data = new Data { Id = payment.MpPaymentId }
        });

        var savedReservation = await db.Reservations.FindAsync(reservation.Id);
        var savedPayment = await db.Payments.FindAsync(payment.Id);

        savedReservation!.State.Should().Be(ReservationState.ConfirmedPaymentComplete);
        savedPayment!.PaymentStatus.Should().Be(PaymentStatus.Approved);
    }

    [Fact]
    public async Task Webhook_Rejected_RollsBackToPreviousState()
    {
        using var db = CreateDbContext();
        var user = CreateUser();
        var apartment = CreateApartment(1000m);
        var reservation = CreateReservation(user, apartment, ReservationState.PaymentPending);
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            Amount = 350m,
            IsSign = true,
            PaymentStatus = PaymentStatus.Pending,
            MpPaymentId = "789",
            ReservationId = reservation.Id,
            AppUserId = user.Id,
            PaymentDate = DateTime.UtcNow,
            PreviousReservationState = ReservationState.ConfirmedPaymentIncomplete
        };

        db.Apartments.Add(apartment);
        db.AppUsers.Add(user);
        db.Reservations.Add(reservation);
        db.Payments.Add(payment);
        await db.SaveChangesAsync();

        var bankService = new Mock<IBankService>();
        bankService.Setup(x => x.GetAsync(It.IsAny<long>())).ReturnsAsync("rejected");

        var paymentService = CreateService(db, bankService.Object);

        await paymentService.CardPaymentWebhook(new PaymentWebhookRequest
        {
            Type = "payment",
            Data = new Data { Id = payment.MpPaymentId }
        });

        var savedReservation = await db.Reservations.FindAsync(reservation.Id);
        var savedPayment = await db.Payments.FindAsync(payment.Id);

        savedReservation!.State.Should().Be(ReservationState.ConfirmedPaymentIncomplete);
        savedPayment!.PaymentStatus.Should().Be(PaymentStatus.Rejected);
    }

    [Fact]
    public async Task Webhook_Cancelled_RollsBackToPreviousStateOrCancelled()
    {
        using var db = CreateDbContext();
        var user = CreateUser();
        var apartment = CreateApartment(1000m);
        var reservation = CreateReservation(user, apartment, ReservationState.PaymentPending);
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            Amount = 350m,
            IsSign = true,
            PaymentStatus = PaymentStatus.Pending,
            MpPaymentId = "101",
            ReservationId = reservation.Id,
            AppUserId = user.Id,
            PaymentDate = DateTime.UtcNow,
            PreviousReservationState = null
        };

        db.Apartments.Add(apartment);
        db.AppUsers.Add(user);
        db.Reservations.Add(reservation);
        db.Payments.Add(payment);
        await db.SaveChangesAsync();

        var bankService = new Mock<IBankService>();
        bankService.Setup(x => x.GetAsync(It.IsAny<long>())).ReturnsAsync("cancelled");

        var paymentService = CreateService(db, bankService.Object);

        await paymentService.CardPaymentWebhook(new PaymentWebhookRequest
        {
            Type = "payment",
            Data = new Data { Id = payment.MpPaymentId }
        });

        var savedReservation = await db.Reservations.FindAsync(reservation.Id);
        var savedPayment = await db.Payments.FindAsync(payment.Id);

        savedReservation!.State.Should().Be(ReservationState.Cancelled);
        savedPayment!.PaymentStatus.Should().Be(PaymentStatus.Cancelled);
    }

    [Fact]
    public async Task Webhook_SameStatus_DoesNotChangeState()
    {
        using var db = CreateDbContext();
        var user = CreateUser();
        var apartment = CreateApartment(1000m);
        var reservation = CreateReservation(user, apartment, ReservationState.PaymentPending);
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            Amount = 350m,
            IsSign = false,
            PaymentStatus = PaymentStatus.Approved,
            MpPaymentId = "202",
            ReservationId = reservation.Id,
            AppUserId = user.Id,
            PaymentDate = DateTime.UtcNow,
            PreviousReservationState = null
        };

        db.Apartments.Add(apartment);
        db.AppUsers.Add(user);
        db.Reservations.Add(reservation);
        db.Payments.Add(payment);
        await db.SaveChangesAsync();

        var bankService = new Mock<IBankService>();
        bankService.Setup(x => x.GetAsync(It.IsAny<long>())).ReturnsAsync("approved");

        var paymentService = CreateService(db, bankService.Object);

        await paymentService.CardPaymentWebhook(new PaymentWebhookRequest
        {
            Type = "payment",
            Data = new Data { Id = payment.MpPaymentId }
        });

        var savedReservation = await db.Reservations.FindAsync(reservation.Id);
        var savedPayment = await db.Payments.FindAsync(payment.Id);

        savedReservation!.State.Should().Be(ReservationState.PaymentPending);
        savedPayment!.PaymentStatus.Should().Be(PaymentStatus.Approved);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static PaymentService CreateService(AppDbContext db, IBankService bankService, IReservationService? reservationService = null)
    {
        var mailService = new Mock<IMailService>();
        reservationService ??= CreateAvailableReservationService();

        return new PaymentService(db, reservationService, bankService, Mock.Of<IUserService>(), NullLogger<GlobalExceptionHandler>.Instance, mailService.Object);
    }

    private static IReservationService CreateAvailableReservationService()
    {
        var reservationService = new Mock<IReservationService>();
        reservationService
            .Setup(x => x.DatesAreInvalid(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<Guid>(), It.IsAny<Guid?>()))
            .ReturnsAsync(false);
        return reservationService.Object;
    }

    private static AppUser CreateUser()
    {
        return new AppUser
        {
            Id = Guid.NewGuid(),
            Name = "Test",
            LastName = "User",
            Email = "test@test.com",
            Telefono = "123",
            Usertype = UserType.User,
            IsDeleted = false
        };
    }

    private static Apartment CreateApartment(decimal price)
    {
        return new Apartment
        {
            Id = Guid.NewGuid(),
            Name = "Apartment",
            Price = price,
            Location = "Address",
            IsDeleted = false
        };
    }

    private static Reservation CreateReservation(AppUser user, Apartment apartment, ReservationState state)
    {
        var checkIn = new DateTime(2026, 1, 1);
        var checkOut = new DateTime(2026, 1, 3);

        return new Reservation
        {
            Id = Guid.NewGuid(),
            ApartmentId = apartment.Id,
            Apartment = apartment,
            AppUserId = user.Id,
            AppUser = user,
            CheckInDate = checkIn,
            CheckOutDate = checkOut,
            State = state,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsDeleted = false,
            Payments = []
        };
    }
}
