namespace GenerationalJournal.Api.Endpoints;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using GenerationalJournal.Application.Services;

public static class MediaEndpoints
{
    public static void MapMediaEndpoints(this WebApplication app)
    {
        var entriesGroup = app.MapGroup("/api/entries").RequireAuthorization();
        var mediaGroup = app.MapGroup("/api/media").RequireAuthorization();

        entriesGroup.MapPost("/{id:guid}/media", async (
            Guid id,
            IFormFile file,
            IMediaService mediaService,
            ClaimsPrincipal user) =>
        {
            if (file is null || file.Length == 0)
            {
                return Results.BadRequest(new { error = "A file is required." });
            }

            try
            {
                var userId = GetUserId(user);
                await using var stream = file.OpenReadStream();
                var response = await mediaService.UploadMediaAsync(
                    id, userId, file.FileName, file.ContentType, file.Length, stream);
                return Results.Created($"/api/media/{response.Id}", response);
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Forbid();
            }
        });

        entriesGroup.MapGet("/{id:guid}/media", async (
            Guid id,
            IMediaService mediaService,
            ClaimsPrincipal user) =>
        {
            try
            {
                var userId = GetUserId(user);
                var media = await mediaService.GetMediaByEntryAsync(id, userId);
                return Results.Ok(media);
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

        mediaGroup.MapDelete("/{id:guid}", async (
            Guid id,
            IMediaService mediaService,
            ClaimsPrincipal user) =>
        {
            try
            {
                var userId = GetUserId(user);
                await mediaService.DeleteMediaAsync(id, userId);
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

        mediaGroup.MapGet("/{id:guid}/file", async (
            Guid id,
            IMediaService mediaService,
            ClaimsPrincipal user) =>
        {
            try
            {
                var userId = GetUserId(user);
                var (content, contentType, fileName) = await mediaService.GetMediaFileAsync(id, userId);
                return Results.File(content, contentType, fileName);
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
