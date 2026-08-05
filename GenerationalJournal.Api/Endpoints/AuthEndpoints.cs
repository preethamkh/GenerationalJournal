namespace GenerationalJournal.Api.Endpoints;

using System.ComponentModel.DataAnnotations;
using GenerationalJournal.Application.DTOs.Auth;
using GenerationalJournal.Application.Services;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        app.MapPost("/api/auth/register", async (RegisterRequest request, IAuthService authService) =>
        {
            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(request);
            if (!Validator.TryValidateObject(request, validationContext, validationResults, true))
            {
                var errors = validationResults.Select(v => v.ErrorMessage);
                return Results.BadRequest(new { errors });
            }

            try
            {
                var response = await authService.RegisterAsync(request);
                return Results.Created($"/api/auth/users/{response.UserId}", response);
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(new { error = ex.Message });
            }
        });

        app.MapPost("/api/auth/login", async (LoginRequest request, IAuthService authService) =>
        {
            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(request);
            if (!Validator.TryValidateObject(request, validationContext, validationResults, true))
            {
                var errors = validationResults.Select(v => v.ErrorMessage);
                return Results.BadRequest(new { errors });
            }

            try
            {
                var response = await authService.LoginAsync(request);
                return Results.Ok(response);
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Unauthorized();
            }
        });
    }
}
