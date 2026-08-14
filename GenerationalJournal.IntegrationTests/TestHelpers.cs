using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using GenerationalJournal.Application.DTOs.Auth;

namespace GenerationalJournal.IntegrationTests;

public static class TestHelpers
{
    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static async Task<AuthResponse> RegisterAsync(
        this HttpClient client,
        string email,
        string password = "Password123!",
        string firstName = "Jane",
        string lastName = "Doe")
    {
        var response = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Email = email,
            Password = password,
            FirstName = firstName,
            LastName = lastName
        }, JsonOptions);

        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions))!;
    }

    public static void Authenticate(this HttpClient client, string token)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }
}
