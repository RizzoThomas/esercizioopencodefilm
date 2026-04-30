using FilmAPI.DTO;
using FilmAPI.Services;
using System.Security.Claims;

namespace FilmAPI.Endpoints;

public static class ValidazioneBigliettiEndpoints
{
    public static void MapValidazioneBigliettiEndpoints(this WebApplication app)
    {
        var validationGroup = app.MapGroup("/admin/tickets/validate")
            .RequireAuthorization("PowerUserOrAdmin");

        validationGroup.MapGet("/{code}", async (string code, IValidazioneBigliettoService service) =>
        {
            try
            {
                var result = await service.GetTicketByCodeAsync(code);
                return result is null ? Results.NotFound() : Results.Ok(result);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });

        validationGroup.MapPost(string.Empty, async (
            TicketValidationRequestDTO dto,
            ClaimsPrincipal user,
            IValidazioneBigliettoService service) =>
        {
            var operatorUserId = int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            if (operatorUserId == 0)
                return Results.Unauthorized();

            try
            {
                var result = await service.ValidateAsync(operatorUserId, dto);
                return Results.Ok(result);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(ex.Message);
            }
        });
    }
}
