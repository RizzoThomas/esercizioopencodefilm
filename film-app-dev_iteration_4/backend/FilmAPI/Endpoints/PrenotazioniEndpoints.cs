using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FilmAPI.DTO;
using FilmAPI.Services;

namespace FilmAPI.Endpoints;

public static class PrenotazioniEndpoints
{
    public static void MapPrenotazioniEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/prenotazioni");

        group.MapGet("", async (HttpContext context, IPrenotazioneService service, IAuthService authService) =>
        {
            var userId = GetUserIdFromContext(context);
            if (userId == null) return Results.Unauthorized();

            var user = await authService.GetUserByIdAsync(userId.Value);
            if (user is null) return Results.Unauthorized();

            var isAdmin = user.Ruolo == "Admin";
            var prenotazioni = isAdmin
                ? await service.GetAllPrenotazioniAsync()
                : await service.GetPrenotazioniAsync(userId.Value);

            return Results.Ok(prenotazioni);
        }).RequireAuthorization("Authenticated");

        group.MapPost("", async (HttpContext context, PrenotazioneCreateDTO dto, IPrenotazioneService service) =>
        {
            var userId = GetUserIdFromContext(context);
            if (userId == null) return Results.Unauthorized();

            try
            {
                var result = await service.CreatePrenotazioneAsync(userId.Value, dto);
                return Results.Created($"/prenotazioni/{result?.Id}", result);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        }).RequireAuthorization("Authenticated");

        group.MapDelete("/{id}", async (HttpContext context, int id, IPrenotazioneService service) =>
        {
            var userId = GetUserIdFromContext(context);
            if (userId == null) return Results.Unauthorized();

            var result = await service.DeletePrenotazioneAsync(userId.Value, id);
            return result ? Results.NoContent() : Results.NotFound();
        }).RequireAuthorization("Authenticated");
    }

    private static int? GetUserIdFromContext(HttpContext context)
    {
        var userIdClaim = context.User.FindFirst("sub")?.Value
            ?? context.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
            ?? context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return null;
        }

        return userId;
    }
}
