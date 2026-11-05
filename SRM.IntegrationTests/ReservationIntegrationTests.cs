using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using SRM.Api.Data;
using SRM.Api.Models.Dto.Apartment;
using SRM.Api.Models.Dto.Reservation;
using SRM.Api.Models.Dto.User;
using SRM.Api.Models.Entities;
using SRM.Api.Models.Enums;
using System.Net;
using System.Net.Http.Json;

namespace SRM.IntegrationTests;

public class ReservationIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ReservationIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private static MultipartFormDataContent BuildApartmentMultipartPayload()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var content = new MultipartFormDataContent();

        content.Add(new StringContent($"Depto {suffix}"), "Name");
        content.Add(new StringContent($"Descripcion valida para el departamento {suffix}. Debe tener al menos 10 caracteres para pasar la validacion del backend."), "Description");
        content.Add(new StringContent("2500"), "Price");
        content.Add(new StringContent($"Calle {suffix} 123"), "Location");
        content.Add(new StringContent("-34.6037"), "Latitude");
        content.Add(new StringContent("-58.3816"), "Longitude");

        byte[] jpegBytes = { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00 };
        var fileContent = new ByteArrayContent(jpegBytes);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
        content.Add(fileContent, "Images", "test.jpg");

        return content;
    }

    private async Task<HttpClient> CreateUserClientAsync()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

        var email = $"user{Guid.NewGuid():N}@example.com";
        var password = "Password123!";

        var registerResponse = await client.PostAsJsonAsync("/api/User/register", new CreateUserRequestDto
        {
            FirstName = "User",
            LastName = "Test",
            Telefono = "1123456789",
            Email = email,
            Pwd = password
        });

        var registerBody = await registerResponse.Content.ReadAsStringAsync();
        registerResponse.StatusCode.Should().BeOneOf(new[] { HttpStatusCode.OK, HttpStatusCode.Created }, "Error body: {0}", registerBody);

        var loginResponse = await client.PostAsJsonAsync("/api/User/login", new LoginUserRequestDto
        {
            Email = email,
            Pwd = password
        });

        var loginBody = await loginResponse.Content.ReadAsStringAsync();
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK, "Error body: {0}", loginBody);
        loginResponse.Headers.TryGetValues("Set-Cookie", out var cookies).Should().BeTrue();
        cookies.Should().Contain(cookie => cookie.Contains("X-Access-Token="));

        return client;
    }

    private async Task<HttpClient> CreateAdminClientAsync()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

        var email = $"admin{Guid.NewGuid():N}@example.com";
        var password = "Password123!";

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            db.AppUsers.Add(new AppUser
            {
                Id = Guid.NewGuid(),
                Name = "Admin",
                LastName = "User",
                Email = email,
                Telefono = "1123456789",
                PwdHash = BCrypt.Net.BCrypt.HashPassword(password),
                Usertype = UserType.Admin
            });

            await db.SaveChangesAsync();
        }

        var loginResponse = await client.PostAsJsonAsync("/api/User/login", new LoginUserRequestDto
        {
            Email = email,
            Pwd = password
        });

        var loginBody = await loginResponse.Content.ReadAsStringAsync();
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK, "Error body: {0}", loginBody);
        loginResponse.Headers.TryGetValues("Set-Cookie", out var cookies).Should().BeTrue();
        cookies.Should().Contain(cookie => cookie.Contains("X-Access-Token="));

        return client;
    }

    private async Task<Guid> CreateApartmentAsync(HttpClient adminClient)
    {
        using var payload = BuildApartmentMultipartPayload();
        var response = await adminClient.PostAsync("/api/Apartment", payload);
        var responseBody = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().BeOneOf(new[] { HttpStatusCode.OK, HttpStatusCode.Created }, "Error body: {0}", responseBody);

        var apartment = await response.Content.ReadFromJsonAsync<Apartment>();
        apartment.Should().NotBeNull();
        apartment!.Id.Should().NotBe(Guid.Empty);

        return apartment.Id;
    }

    private static async Task<Guid> CreateReservationAsync(HttpClient client, Guid apartmentId, DateTime checkInDate, DateTime checkOutDate)
    {
        var response = await client.PostAsJsonAsync("/api/Reservation/with_auth", new CreateReservationRequestDto
        {
            ApartmentId = apartmentId,
            CheckInDate = checkInDate,
            CheckOutDate = checkOutDate
        });

        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().BeOneOf(new[] { HttpStatusCode.OK, HttpStatusCode.Created }, "Error body: {0}", body);

        var reservation = await response.Content.ReadFromJsonAsync<Reservation>();
        reservation.Should().NotBeNull();
        reservation!.Id.Should().NotBe(Guid.Empty);

        return reservation.Id;
    }

    private static async Task<Guid> CreateReservationNoAuthAsync(HttpClient client, Guid apartmentId, DateTime checkInDate, DateTime checkOutDate)
    {
        var response = await client.PostAsJsonAsync("/api/Reservation", new CreateReservationRequestDto
        {
            ApartmentId = apartmentId,
            CheckInDate = checkInDate,
            CheckOutDate = checkOutDate
        });

        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().BeOneOf(new[] { HttpStatusCode.OK, HttpStatusCode.Created }, "Error body: {0}", body);

        var reservation = await response.Content.ReadFromJsonAsync<Reservation>();
        reservation.Should().NotBeNull();
        reservation!.Id.Should().NotBe(Guid.Empty);

        return reservation.Id;
    }


    [Fact]
    public async Task CreateReservation_WithAuth_ReturnsSuccess()
    {
        using var adminClient = await CreateAdminClientAsync();
        var apartmentId = await CreateApartmentAsync(adminClient);

        using var userClient = await CreateUserClientAsync();
        var response = await userClient.PostAsJsonAsync("/api/Reservation/with_auth", new CreateReservationRequestDto
        {
            ApartmentId = apartmentId,
            CheckInDate = new DateTime(2026, 10, 17),
            CheckOutDate = new DateTime(2026, 10, 20)
        });

        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().BeOneOf(new[] { HttpStatusCode.OK, HttpStatusCode.Created }, "Error body: {0}", body);

        var reservation = await response.Content.ReadFromJsonAsync<Reservation>();
        reservation.Should().NotBeNull();
        reservation!.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task CreateReservation_OverlappingDates_ReturnsBadRequest()
    {
        using var adminClient = await CreateAdminClientAsync();
        var apartmentId = await CreateApartmentAsync(adminClient);

        using var userClient = await CreateUserClientAsync();

        await CreateReservationNoAuthAsync(userClient, apartmentId, new DateTime(2026, 10, 17), new DateTime(2026, 10, 20));

        var overlapResponse = await userClient.PostAsJsonAsync("/api/Reservation/with_auth", new CreateReservationRequestDto
        {
            ApartmentId = apartmentId,
            CheckInDate = new DateTime(2026, 10, 18),
            CheckOutDate = new DateTime(2026, 10, 22)
        });

        var overlapBody = await overlapResponse.Content.ReadAsStringAsync();
        overlapResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest, "Error body: {0}", overlapBody);
    }

    [Fact]
    public async Task GetReservation_ByIdor_ReturnsForbidden()
    {
        using var adminClient = await CreateAdminClientAsync();
        var apartmentId = await CreateApartmentAsync(adminClient);

        using var userAClient = await CreateUserClientAsync();
        var reservationId = await CreateReservationAsync(userAClient, apartmentId, new DateTime(2026, 10, 17), new DateTime(2026, 10, 20));

        using var userBClient = await CreateUserClientAsync();
        var response = await userBClient.GetAsync($"/api/Reservation/{reservationId}");
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().BeOneOf(new[] { HttpStatusCode.Forbidden, HttpStatusCode.NotFound }, "Error body: {0}", body);
    }

    [Fact]
    public async Task CancelReservation_ByOwner_ReturnsSuccess()
    {
        // BUG CONOCIDO: Lógica invertida
        using var adminClient = await CreateAdminClientAsync();
        var apartmentId = await CreateApartmentAsync(adminClient);

        using var userClient = await CreateUserClientAsync();
        var reservationId = await CreateReservationAsync(userClient, apartmentId, new DateTime(2026, 11, 17), new DateTime(2026, 11, 20));

        var response = await userClient.PostAsync($"/api/Reservation/cancel/{reservationId}", null);
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK, "Error body: {0}", body);
    }
}
