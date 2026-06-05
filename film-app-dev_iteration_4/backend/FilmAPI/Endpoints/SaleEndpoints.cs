using FilmAPI.DTO;
using FilmAPI.Services;

namespace FilmAPI.Endpoints;

/// <summary>
/// Raggruppa gli endpoint pubblici e protetti per la gestione delle sale e delle sale per cinema.
/// </summary>
public static class SaleEndpoints
{
    /// <summary>
    /// Mappa le rotte <c>/cinemas/{cinemaId}/sale</c> e <c>/sale</c> per consultare e gestire le sale.
    /// Alcune rotte sono pubbliche con <c>AllowAnonymous</c>; le operazioni di modifica richiedono <c>RequireAuthorization("PowerUserOrAdmin")</c>.
    /// Esegue operazioni CRUD sulle sale con effetti sul database e sulla configurazione delle sale per cinema.
    /// </summary>
    /// <param name="app">Applicazione web su cui registrare gli endpoint.</param>
    /// <returns>Non restituisce valori.</returns>
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
