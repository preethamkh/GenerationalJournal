using System.Security.Claims;
using System.Text.Json;
using GenerationalJournal.Web.Models;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace GenerationalJournal.Web.Services;

public class AuthStateProvider : AuthenticationStateProvider
{
    private const string AuthKey = "gj.auth";

    private readonly ProtectedLocalStorage _localStorage;
    private AuthResponse? _current;

    public AuthStateProvider(ProtectedLocalStorage localStorage)
    {
        _localStorage = localStorage;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (_current is not null)
        {
            return new AuthenticationState(BuildPrincipal(_current));
        }

        try
        {
            var result = await _localStorage.GetAsync<string>(AuthKey);
            if (result.Success && !string.IsNullOrWhiteSpace(result.Value))
            {
                _current = JsonSerializer.Deserialize<AuthResponse>(result.Value);
                if (_current is not null)
                {
                    return new AuthenticationState(BuildPrincipal(_current));
                }
            }
        }
        catch (InvalidOperationException)
        {
            // Storage is unavailable (e.g. prerendering). Fall through to anonymous.
        }

        return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
    }

    public async Task LoginAsync(AuthResponse response)
    {
        _current = response;
        await _localStorage.SetAsync(AuthKey, JsonSerializer.Serialize(response));
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(BuildPrincipal(response))));
    }

    public async Task LogoutAsync()
    {
        _current = null;
        await _localStorage.DeleteAsync(AuthKey);
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()))));
    }

    public async Task<string?> GetTokenAsync()
    {
        if (_current is not null)
        {
            return _current.Token;
        }

        await GetAuthenticationStateAsync();
        return _current?.Token;
    }

    private static ClaimsPrincipal BuildPrincipal(AuthResponse response)
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, response.UserId.ToString()),
            new Claim(ClaimTypes.Email, response.Email),
            new Claim(ClaimTypes.Name, response.FirstName),
            new Claim(ClaimTypes.GivenName, response.FirstName),
        }, "JWT");

        return new ClaimsPrincipal(identity);
    }
}
