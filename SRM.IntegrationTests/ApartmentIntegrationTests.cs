using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using SRM.Api.Data;
using SRM.Api.Models.Dto.Apartment;
using SRM.Api.Models.Dto.User;
using SRM.Api.Models.Entities;
using SRM.Api.Models.Enums;

namespace SRM.IntegrationTests;

public class ApartmentIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ApartmentIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private static CreateApartmentDto BuildApartmentPayload()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        return new CreateApartmentDto
        {
            Name = $"Depto {suffix}",
            Description = $"Descripcion valida para el departamento {suffix}. Debe tener al menos 10 caracteres para pasar la validacion del backend.",
            Price = 2500m,
            Location = $"Calle {suffix} 123",
            CoverImgUrl = "https://example.com/images/cover.jpg",
            Latitude = -34.6037,
            Longitude = -58.3816
        };
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

    private async Task<Guid> CreateApartmentAsync(HttpClient client)
    {
        var payload = BuildApartmentPayload();
        var response = await client.PostAsJsonAsync("/api/Apartment", payload);
        var responseBody = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().BeOneOf(new[] { HttpStatusCode.OK, HttpStatusCode.Created }, "Error body: {0}", responseBody);

        var apartment = await response.Content.ReadFromJsonAsync<Apartment>();
        apartment.Should().NotBeNull();
        apartment!.Id.Should().NotBe(Guid.Empty);

        return apartment.Id;
    }

    [Fact]
    public async Task CreateApartment_AsAdmin_ReturnsSuccess()
    {
        using var client = await CreateAdminClientAsync();
        var response = await client.PostAsJsonAsync("/api/Apartment", BuildApartmentPayload());
        var responseBody = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().BeOneOf(new[] { HttpStatusCode.OK, HttpStatusCode.Created }, "Error body: {0}", responseBody);
    }

    [Fact]
    public async Task CreateApartment_WithoutAuth_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

        var response = await client.PostAsJsonAsync("/api/Apartment", BuildApartmentPayload());
        var responseBody = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized, "Error body: {0}", responseBody);
    }

    [Fact]
    public async Task SoftDeleteApartment_RemovesFromListing()
    {
        using var client = await CreateAdminClientAsync();
        var apartmentId = await CreateApartmentAsync(client);

        var deleteResponse = await client.DeleteAsync($"/api/Apartment/{apartmentId}");
        var deleteBody = await deleteResponse.Content.ReadAsStringAsync();
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK, "Error body: {0}", deleteBody);

        var listingResponse = await client.GetAsync("/api/Apartment");
        var listingBody = await listingResponse.Content.ReadAsStringAsync();
        listingResponse.StatusCode.Should().Be(HttpStatusCode.OK, "Error body: {0}", listingBody);

        var apartments = await listingResponse.Content.ReadFromJsonAsync<List<ApartmentListingDto>>();
        apartments.Should().NotBeNull();
        apartments!.Should().NotContain(a => a.Id == apartmentId);
    }

    [Fact]
    public async Task UploadImage_WithValidFile_ReturnsSuccess()
    {
        using var client = await CreateAdminClientAsync();
        var apartmentId = await CreateApartmentAsync(client);

        var bytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00 };
        using var content = new MultipartFormDataContent();
        using var fileContent = new ByteArrayContent(bytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        content.Add(fileContent, "Images", "photo.jpg");

        var response = await client.PostAsync($"/api/Apartment/{apartmentId}/images", content);
        var responseBody = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK, "Error body: {0}", responseBody);
    }

    [Fact]
    public async Task UploadImage_WithInvalidExtension_ReturnsBadRequest()
    {
        using var client = await CreateAdminClientAsync();
        var apartmentId = await CreateApartmentAsync(client);

        var bytes = "este no es una imagen real"u8.ToArray();
        using var content = new MultipartFormDataContent();
        using var fileContent = new ByteArrayContent(bytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        content.Add(fileContent, "Images", "mal.txt");

        var response = await client.PostAsync($"/api/Apartment/{apartmentId}/images", content);
        var responseBody = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, "Error body: {0}", responseBody);
    }

    [Fact]
    public async Task GetImage_WithPathTraversal_ReturnsNotFound()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

        var response = await client.GetAsync("/images/..%2F..%2Fappsettings.json");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
