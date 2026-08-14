using System.Net;
using System.Net.Http.Json;
using GenerationalJournal.Application.DTOs.Auth;

namespace GenerationalJournal.IntegrationTests;

public class AuthEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthEndpointsTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_WithValidRequest_ReturnsCreatedWithToken()
    {
        var email = $"register-{Guid.NewGuid()}@example.com";

        var response = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Email = email,
            Password = "Password123!",
            FirstName = "Jane",
            LastName = "Doe"
        }, TestHelpers.JsonOptions);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>(TestHelpers.JsonOptions);
        Assert.NotNull(auth);
        Assert.Equal(email, auth!.Email);
        Assert.Equal("Jane", auth.FirstName);
        Assert.False(string.IsNullOrWhiteSpace(auth.Token));
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ReturnsConflict()
    {
        var email = $"duplicate-{Guid.NewGuid()}@example.com";
        await _client.RegisterAsync(email);

        var response = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Email = email,
            Password = "Password123!",
            FirstName = "Jane",
            LastName = "Doe"
        }, TestHelpers.JsonOptions);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithInvalidData_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Email = "not-an-email",
            Password = "short",
            FirstName = "",
            LastName = ""
        }, TestHelpers.JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsToken()
    {
        var email = $"login-{Guid.NewGuid()}@example.com";
        var password = "Password123!";
        await _client.RegisterAsync(email, password);

        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = email,
            Password = password
        }, TestHelpers.JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>(TestHelpers.JsonOptions);
        Assert.NotNull(auth);
        Assert.False(string.IsNullOrWhiteSpace(auth!.Token));
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        var email = $"badlogin-{Guid.NewGuid()}@example.com";
        await _client.RegisterAsync(email, "Password123!");

        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = email,
            Password = "WrongPassword123!"
        }, TestHelpers.JsonOptions);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
