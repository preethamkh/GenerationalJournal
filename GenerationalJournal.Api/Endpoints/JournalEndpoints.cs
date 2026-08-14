namespace GenerationalJournal.Api.Endpoints;

using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using GenerationalJournal.Application.DTOs.Journal;
using GenerationalJournal.Application.Services;

public static class JournalEndpoints
{
    public static void MapJournalEndpoints(this WebApplication app)
    {
        var familiesGroup = app.MapGroup("/api/families").RequireAuthorization();
        var entriesGroup = app.MapGroup("/api/entries").RequireAuthorization();

        familiesGroup.MapPost("/{familyId:guid}/entries", async (
            Guid familyId,
            CreateJournalEntryRequest request,
            IJournalService journalService,
            ClaimsPrincipal user) =>
        {
            if (!TryValidate(request, out var errors))
            {
                return Results.BadRequest(new { errors });
            }

            try
            {
                var userId = GetUserId(user);
                var response = await journalService.CreateEntryAsync(familyId, request, userId);
                return Results.Created($"/api/entries/{response.Id}", response);
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { error = ex.Message });
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Forbid();
            }
        });

        familiesGroup.MapGet("/{familyId:guid}/entries", async (
            Guid familyId,
            int? page,
            int? pageSize,
            DateTime? fromDate,
            DateTime? toDate,
            string? mood,
            IJournalService journalService,
            ClaimsPrincipal user) =>
        {
            try
            {
                var userId = GetUserId(user);
                var response = await journalService.GetEntriesByFamilyAsync(
                    familyId, userId, page ?? 1, pageSize ?? 20, fromDate, toDate, mood);
                return Results.Ok(response);
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { error = ex.Message });
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Forbid();
            }
        });

        entriesGroup.MapGet("/search", async (
            string? q,
            int? page,
            int? pageSize,
            IJournalService journalService,
            ClaimsPrincipal user) =>
        {
            try
            {
                var userId = GetUserId(user);
                var response = await journalService.SearchEntriesAsync(q ?? string.Empty, userId, page ?? 1, pageSize ?? 20);
                return Results.Ok(response);
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Forbid();
            }
        });

        entriesGroup.MapGet("/{id:guid}", async (
            Guid id,
            IJournalService journalService,
            ClaimsPrincipal user) =>
        {
            try
            {
                var userId = GetUserId(user);
                var response = await journalService.GetEntryByIdAsync(id, userId);
                return Results.Ok(response);
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { error = ex.Message });
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Forbid();
            }
        });

        entriesGroup.MapPut("/{id:guid}", async (
            Guid id,
            UpdateJournalEntryRequest request,
            IJournalService journalService,
            ClaimsPrincipal user) =>
        {
            if (!TryValidate(request, out var errors))
            {
                return Results.BadRequest(new { errors });
            }

            try
            {
                var userId = GetUserId(user);
                var response = await journalService.UpdateEntryAsync(id, request, userId);
                return Results.Ok(response);
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { error = ex.Message });
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Forbid();
            }
        });

        entriesGroup.MapDelete("/{id:guid}", async (
            Guid id,
            IJournalService journalService,
            ClaimsPrincipal user) =>
        {
            try
            {
                var userId = GetUserId(user);
                await journalService.DeleteEntryAsync(id, userId);
                return Results.NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { error = ex.Message });
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Forbid();
            }
        });
    }

    private static bool TryValidate(object request, out List<string> errors)
    {
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(request);
        if (!Validator.TryValidateObject(request, validationContext, validationResults, true))
        {
            errors = validationResults.Select(v => v.ErrorMessage ?? string.Empty).ToList();
            return false;
        }

        errors = new List<string>();
        return true;
    }

    private static Guid GetUserId(ClaimsPrincipal user)
    {
        var claim = user.FindFirst(ClaimTypes.NameIdentifier) ?? user.FindFirst(JwtRegisteredClaimNames.Sub);
        return claim is not null && Guid.TryParse(claim.Value, out var id) ? id : Guid.Empty;
    }
}
