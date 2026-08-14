namespace GenerationalJournal.Api.Endpoints;

using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using GenerationalJournal.Application.DTOs.Family;
using GenerationalJournal.Application.Services;

public static class FamilyEndpoints
{
    public static void MapFamilyEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/families").RequireAuthorization();

        group.MapPost("", async (CreateFamilyRequest request, IFamilyService familyService, ClaimsPrincipal user) =>
        {
            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(request);
            if (!Validator.TryValidateObject(request, validationContext, validationResults, true))
            {
                var errors = validationResults.Select(v => v.ErrorMessage);
                return Results.BadRequest(new { errors });
            }

            var userId = GetUserId(user);
            var response = await familyService.CreateFamilyAsync(request, userId);
            return Results.Created($"/api/families/{response.Id}", response);
        });

        group.MapGet("", async (IFamilyService familyService, ClaimsPrincipal user) =>
        {
            var userId = GetUserId(user);
            var families = await familyService.GetUserFamiliesAsync(userId);
            return Results.Ok(families);
        });

        group.MapGet("/{id:guid}", async (Guid id, IFamilyService familyService, ClaimsPrincipal user) =>
        {
            try
            {
                var userId = GetUserId(user);
                var family = await familyService.GetFamilyByIdAsync(id, userId);
                return Results.Ok(family);
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

        group.MapPost("/{id:guid}/members", async (Guid id, AddMemberRequest request, IFamilyService familyService, ClaimsPrincipal user) =>
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
                var userId = GetUserId(user);
                var member = await familyService.AddMemberAsync(id, request, userId);
                return Results.Created($"/api/families/{id}/members/{member.UserId}", member);
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(new { error = ex.Message });
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Forbid();
            }
        });

        group.MapGet("/{id:guid}/members", async (Guid id, IFamilyService familyService, ClaimsPrincipal user) =>
        {
            try
            {
                var userId = GetUserId(user);
                var members = await familyService.GetFamilyMembersAsync(id, userId);
                return Results.Ok(members);
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

        group.MapDelete("/{id:guid}/members/{userId:guid}", async (Guid id, Guid userId, IFamilyService familyService, ClaimsPrincipal user) =>
        {
            try
            {
                var requesterUserId = GetUserId(user);
                await familyService.RemoveMemberAsync(id, userId, requesterUserId);
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

    private static Guid GetUserId(ClaimsPrincipal user)
    {
        var claim = user.FindFirst(ClaimTypes.NameIdentifier) ?? user.FindFirst(JwtRegisteredClaimNames.Sub);
        return claim is not null && Guid.TryParse(claim.Value, out var id) ? id : Guid.Empty;
    }
}
