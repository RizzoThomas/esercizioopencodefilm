using FilmAPI.DTO;
using FilmAPI.Services;

namespace FilmAPI.Endpoints;

public static class SaleEndpoints
{
    public static void MapSaleEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/cinemas/{cinemaId}/sale");

        group.MapGet("", async (int cinemaId, ISalaService service) =>
            await service.GetByCinemaAsync(cinemaId))
            .AllowAnonymous();

        group.MapPost("", async (int cinemaId, SalaCreateDTO dto, ISalaService service) =>
        {
            if (dto.CinemaId != cinemaId)
                return Results.BadRequest("Il cinemaId nel body non corrisponde a quello nella route.");

            try
            {
                var result = await service.CreateAsync(dto);
                return Results.Created($"/sale/{result.Id}", result);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(ex.Message);
            }
        }).RequireAuthorization("PowerUserOrAdmin");

        var salaGroup = app.MapGroup("/sale");

        salaGroup.MapGet("/{salaId}", async (int salaId, ISalaService service) =>
        {
            var result = await service.GetByIdAsync(salaId);
            return result is null ? Results.NotFound() : Results.Ok(result);
        }).AllowAnonymous();

        salaGroup.MapPut("/{salaId}", async (int salaId, SalaUpdateDTO dto, ISalaService service) =>
        {
            try
            {
                var result = await service.UpdateAsync(salaId, dto);
                return result is null ? Results.NotFound() : Results.Ok(result);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        }).RequireAuthorization("PowerUserOrAdmin");

        salaGroup.MapDelete("/{salaId}", async (int salaId, ISalaService service) =>
        {
            try
            {
                var result = await service.DeleteAsync(salaId);
                return result ? Results.NoContent() : Results.NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(ex.Message);
            }
        }).RequireAuthorization("PowerUserOrAdmin");

        salaGroup.MapGet("/{salaId}/posti", async (int salaId, ISalaService service) =>
        {
            try
            {
                var result = await service.GetPostiAsync(salaId);
                return Results.Ok(result);
            }
            catch (ArgumentException ex)
            {
                return Results.NotFound(ex.Message);
            }
        }).AllowAnonymous();

        salaGroup.MapPut("/{salaId}/posti", async (int salaId, SalaLayoutSaveDTO dto, ISalaService service) =>
        {
            try
            {
                var result = await service.SavePostiAsync(salaId, dto);
                return Results.Ok(result);
            }
            catch (ArgumentException ex)
            {
                return Results.NotFound(ex.Message);
            }
        }).RequireAuthorization("PowerUserOrAdmin");
    }
}
