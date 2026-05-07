using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FilmAPI.DTO;
using FilmAPI.Services;

namespace FilmAPI.Endpoints;

public static class ProfiloEndpoints
{
    public static void MapProfiloEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/profilo");

        group.MapGet("", async (HttpContext context, IProfiloService service) =>
        {
            var userId = GetUserIdFromContext(context);
            if (userId == null) return Results.Unauthorized();

            var profilo = await service.GetProfiloAsync(userId.Value);
            return profilo is null ? Results.NotFound() : Results.Ok(profilo);
        }).RequireAuthorization("Authenticated");

        group.MapPut("", async (HttpContext context, ProfiloUpdateDTO dto, IProfiloService service) =>
        {
            var userId = GetUserIdFromContext(context);
            if (userId == null) return Results.Unauthorized();

            var result = await service.UpdateProfiloAsync(userId.Value, dto);
            return result is null ? Results.NotFound() : Results.Ok(result);
        }).RequireAuthorization("Authenticated");

        group.MapGet("/subscription", async (HttpContext context, IProfiloService service) =>
        {
            var userId = GetUserIdFromContext(context);
            if (userId == null) return Results.Unauthorized();

            var result = await service.GetUserSubscriptionAsync(userId.Value);
            return result is null ? Results.NotFound() : Results.Ok(result);
        }).RequireAuthorization("Authenticated");

        group.MapGet("/vouchers", async (HttpContext context, IProfiloService service) =>
        {
            var userId = GetUserIdFromContext(context);
            if (userId == null) return Results.Unauthorized();

            var result = await service.GetUserVouchersAsync(userId.Value);
            return Results.Ok(result);
        }).RequireAuthorization("Authenticated");

        group.MapPost("/subscription/cancel", async (HttpContext context, IProfiloService service) =>
        {
            var userId = GetUserIdFromContext(context);
            if (userId == null) return Results.Unauthorized();

            var result = await service.CancelUserSubscriptionAsync(userId.Value);
            return result is null ? Results.NotFound() : Results.Ok(result);
        }).RequireAuthorization("Authenticated");

        group.MapPut("/subscription/autorenew", async (HttpContext context, AutoRenewRequestDTO dto, IProfiloService service) =>
        {
            var userId = GetUserIdFromContext(context);
            if (userId == null) return Results.Unauthorized();

            var result = await service.ToggleAutoRenewAsync(userId.Value, dto.AutoRinnovo);
            return result is null ? Results.NotFound() : Results.Ok(result);
        }).RequireAuthorization("Authenticated");

        group.MapGet("/cinema-preferito", async (HttpContext context, IProfiloService service) =>
        {
            var userId = GetUserIdFromContext(context);
            if (userId == null) return Results.Unauthorized();

            var result = await service.GetCinemaPreferitoAsync(userId.Value);
            return result is null ? Results.NotFound() : Results.Ok(result);
        }).RequireAuthorization("Authenticated");

        group.MapPut("/cinema-preferito", async (HttpContext context, IProfiloService service) =>
        {
            var userId = GetUserIdFromContext(context);
            if (userId == null) return Results.Unauthorized();

            try
            {
                var result = await service.SetCinemaPreferitoAsync(userId.Value, null);
                return Results.Ok(result);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return Results.NotFound(ex.Message);
            }
        }).RequireAuthorization("Authenticated");

        group.MapPut("/cinema-preferito/{cinemaId:int}", async (HttpContext context, int cinemaId, IProfiloService service) =>
        {
            var userId = GetUserIdFromContext(context);
            if (userId == null) return Results.Unauthorized();

            try
            {
                var result = await service.SetCinemaPreferitoAsync(userId.Value, cinemaId);
                return Results.Ok(result);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return Results.NotFound(ex.Message);
            }
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

    public class AutoRenewRequestDTO
    {
        public bool AutoRinnovo { get; set; }
    }
}
