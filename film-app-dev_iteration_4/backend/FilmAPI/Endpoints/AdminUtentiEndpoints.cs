using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FilmAPI.DTO;
using FilmAPI.Services;

namespace FilmAPI.Endpoints;

public static class AdminUtentiEndpoints
{
    public static void MapAdminUtentiEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/admin/utenti");

        group.MapGet("", async (IUserAdminService service) =>
            await service.GetAllUsersAsync())
            .RequireAuthorization("AdminOnly");

        group.MapPut("/{id}/ruolo", async (int id, UpdateRuoloDTO dto, HttpContext context, IUserAdminService service) =>
        {
            var requestingUserId = GetUserIdFromContext(context);
            if (requestingUserId == null) return Results.Unauthorized();

            try
            {
                var result = await service.UpdateUserRoleAsync(id, dto, requestingUserId.Value);
                return result is null ? Results.NotFound() : Results.Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        }).RequireAuthorization("AdminOnly");

        group.MapPut("/{id}/credito", async (int id, UpdateCreditoDTO dto, IUserAdminService service) =>
        {
            try
            {
                var result = await service.UpdateUserCreditoAsync(id, dto);
                return result is null ? Results.NotFound() : Results.Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        }).RequireAuthorization("AdminOnly");

        // Admin: visualizza biglietti di un utente specifico
        group.MapGet("/{id}/biglietti", async (int id, ICheckoutService checkoutService) =>
        {
            try
            {
                var biglietti = await checkoutService.GetTicketsByUserAsync(id);
                return Results.Ok(biglietti);
            }
            catch (Exception ex)
            {
                return Results.NotFound(ex.Message);
            }
        }).RequireAuthorization("PowerUserOrAdmin");

        // Admin: visualizza ordini di un utente specifico
        group.MapGet("/{id}/ordini", async (int id, ICheckoutService checkoutService) =>
        {
            try
            {
                var ordini = await checkoutService.GetOrdiniByUserAsync(id);
                return Results.Ok(ordini);
            }
            catch (Exception ex)
            {
                return Results.NotFound(ex.Message);
            }
        }).RequireAuthorization("PowerUserOrAdmin");
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
