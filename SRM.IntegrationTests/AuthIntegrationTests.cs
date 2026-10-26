using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using SRM.Api.Models.Dto.User;

namespace SRM.IntegrationTests;

public class AuthIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AuthIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });
    }

    // La implementación de WebApplicationFactory maneja cookies automáticamente para el cliente HTTP.

    [Fact]
    public async Task GetMe_WithoutAuthentication_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/User/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Register_WithValidData_ReturnsSuccess()
    {
        var request = new CreateUserRequestDto
        {
            FirstName = "Test",
            LastName = "User",
            Telefono = "1123456789",
            Email = $"user{Guid.NewGuid().ToString()[..8]}@example.com",
            Pwd = "Password123!"
        };

        var response = await _client.PostAsJsonAsync("/api/User/register", request);
        var errorContent = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().BeOneOf(new[] { HttpStatusCode.OK, HttpStatusCode.Created }, "Error body: {0}", errorContent);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ReturnsBadRequest()
    {
        var email = $"duplicate{Guid.NewGuid().ToString()[..8]}@example.com";
        var request = new CreateUserRequestDto
        {
            FirstName = "Test",
            LastName = "User",
            Telefono = "1123456789",
            Email = email,
            Pwd = "Password123!"
        };

        var firstResponse = await _client.PostAsJsonAsync("/api/User/register", request);
        var firstErrorContent = await firstResponse.Content.ReadAsStringAsync();
        var secondResponse = await _client.PostAsJsonAsync("/api/User/register", request);
        var secondErrorContent = await secondResponse.Content.ReadAsStringAsync();

        firstResponse.StatusCode.Should().BeOneOf(new[] { HttpStatusCode.OK, HttpStatusCode.Created }, "Error body: {0}", firstErrorContent);
        secondResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest, "Error body: {0}", secondErrorContent);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsSuccessAndCookie()
    {
        var email = $"login{Guid.NewGuid().ToString()[..8]}@example.com";
        var password = "Password123!";

        var registerResponse = await _client.PostAsJsonAsync("/api/User/register", new CreateUserRequestDto
        {
            FirstName = "Login",
            LastName = "User",
            Telefono = "1123456789",
            Email = email,
            Pwd = password
        });
        var registerErrorContent = await registerResponse.Content.ReadAsStringAsync();
        registerResponse.StatusCode.Should().BeOneOf(new[] { HttpStatusCode.OK, HttpStatusCode.Created }, "Error body: {0}", registerErrorContent);

        var response = await _client.PostAsJsonAsync("/api/User/login", new LoginUserRequestDto
        {
            Email = email,
            Pwd = password
        });
        var errorContent = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK, "Error body: {0}", errorContent);
        response.Headers.TryGetValues("Set-Cookie", out var cookies).Should().BeTrue();
        cookies.Should().Contain(cookie => cookie.Contains("X-Access-Token="));
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsBadRequest()
    {
        var request = new LoginUserRequestDto
        {
            Email = $"wrong{Guid.NewGuid().ToString()[..8]}@example.com",
            Pwd = "WrongPassword!"
        };

        var response = await _client.PostAsJsonAsync("/api/User/login", request);
        var errorContent = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, "Error body: {0}", errorContent);
    }
}
