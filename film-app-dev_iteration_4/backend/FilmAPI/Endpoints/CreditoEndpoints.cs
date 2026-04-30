using FilmAPI.DTO;
using FilmAPI.Services;
using System.Security.Claims;

namespace FilmAPI.Endpoints;

public static class CreditoEndpoints
{
    public static void MapCreditoEndpoints(this WebApplication app)
    {
        app.MapGet("/credito/me", async (ClaimsPrincipal user, ICreditoService service) =>
        {
            var userId = int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            if (userId == 0)
                return Results.Unauthorized();

            var result = await service.GetCreditoMeAsync(userId);
            return result is null ? Results.NotFound() : Results.Ok(result);
        }).RequireAuthorization("Authenticated");

        app.MapPost("/credito/topup/stripe-session", async (
            CreateTopupSessionRequestDTO dto,
            HttpContext httpContext,
            ClaimsPrincipal user,
            ICreditoService service) =>
        {
            var userId = int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            if (userId == 0)
                return Results.Unauthorized();

            try
            {
                var result = await service.CreateTopupSessionAsync(userId, dto.Amount);
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

        app.MapPost("/credito/topup/reconcile", async (
            HttpContext httpContext,
            ClaimsPrincipal user,
            ICreditoService service) =>
        {
            var userId = int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            if (userId == 0)
                return Results.Unauthorized();

            var sessionId = httpContext.Request.Query["sessionId"].FirstOrDefault()
                ?? httpContext.Request.Query["session_id"].FirstOrDefault();

            if (string.IsNullOrWhiteSpace(sessionId))
                return Results.BadRequest("sessionId è richiesto.");

            try
            {
                var result = await service.ReconcileTopupSessionAsync(userId, sessionId);
                return result.Success ? Results.Ok(result) : Results.BadRequest(result);
            }
            catch (Exception ex)
            {
                return Results.BadRequest(ex.Message);
            }
        }).RequireAuthorization("Authenticated");

        var adminGroup = app.MapGroup("/admin/credito").RequireAuthorization("PowerUserOrAdmin");

        adminGroup.MapGet("/users", async (string? email, ICreditoService service) =>
        {
            var result = await service.SearchUsersAsync(email);
            return Results.Ok(result);
        });

        adminGroup.MapGet("/ricariche", async (string? email, ICreditoService service) =>
        {
            var result = await service.GetTopUpsAsync(email);
            return Results.Ok(result);
        });

        adminGroup.MapPost("/ricariche", async (
            CreditoTopUpRequestDTO dto,
            ClaimsPrincipal user,
            ICreditoService service) =>
        {
            var operatorUserId = int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            if (operatorUserId == 0)
                return Results.Unauthorized();

            try
            {
                var result = await service.TopUpAsync(operatorUserId, dto);
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
        });
    }
}
