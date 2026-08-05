namespace GenerationalJournal.Application.Services;

using GenerationalJournal.Application.DTOs.Auth;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request);
}
