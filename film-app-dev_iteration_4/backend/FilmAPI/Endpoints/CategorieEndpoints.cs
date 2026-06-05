using FilmAPI.DTO;
using FilmAPI.Services;

namespace FilmAPI.Endpoints;

/// <summary>
/// Raggruppa gli endpoint pubblici e protetti per la gestione delle categorie.
/// </summary>
public static class CategorieEndpoints
{
    /// <summary>
    /// Mappa il gruppo <c>/categorie</c> per elenco, dettaglio, creazione, aggiornamento ed eliminazione delle categorie.
    /// Le rotte di lettura sono pubbliche con <c>AllowAnonymous</c>; le modifiche richiedono <c>RequireAuthorization("PowerUserOrAdmin")</c>.
    /// Esegue operazioni CRUD sulle categorie con effetti sul database.
    /// </summary>
    /// <param name="app">Applicazione web su cui registrare gli endpoint.</param>
    /// <returns>Non restituisce valori.</returns>
    public static void MapCategorieEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/categorie");

        group.MapGet("", async (ICategoriaService service) =>
            await service.GetAllAsync())
            .AllowAnonymous();

        group.MapGet("/{id}", async (int id, ICategoriaService service) =>
        {
            var result = await service.GetByIdAsync(id);
            return result is null ? Results.NotFound() : Results.Ok(result);
        }).AllowAnonymous();

        group.MapPost("", async (CategoriaCreateDTO dto, ICategoriaService service) =>
        {
            try
            {
                var result = await service.CreateAsync(dto);
                return Results.Created($"/categorie/{result.Id}", result);
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

        group.MapPut("/{id}", async (int id, CategoriaUpdateDTO dto, ICategoriaService service) =>
        {
            try
            {
                var result = await service.UpdateAsync(id, dto);
                return result is null ? Results.NotFound() : Results.Ok(result);
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

        group.MapDelete("/{id}", async (int id, ICategoriaService service) =>
        {
            var result = await service.DeleteAsync(id);
            return result ? Results.NoContent() : Results.NotFound();
        }).RequireAuthorization("PowerUserOrAdmin");
    }
}
