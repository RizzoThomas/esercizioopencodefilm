using FilmAPI.DTO;

namespace FilmAPI.Endpoints;

/// <summary>
/// Raggruppa gli endpoint amministrativi per la gestione delle segnalazioni.
/// </summary>
public static class SegnalazioniEndpoints
{
    /// <summary>
    /// Mappa il gruppo <c>/admin/segnalazioni</c> per creare, leggere e aggiornare le segnalazioni.
    /// Richiede <c>RequireAuthorization("PowerUserOrAdmin")</c>.
    /// Esegue operazioni su un archivio in memoria con effetti sullo stato delle segnalazioni gestite.
    /// </summary>
    /// <param name="app">Applicazione web su cui registrare gli endpoint.</param>
    /// <returns>Non restituisce valori.</returns>
    public static void MapSegnalazioniEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/admin/segnalazioni").RequireAuthorization("PowerUserOrAdmin");

        group.MapGet("", () =>
        {
            var list = SegnalazioniStore.GetAll()
                .OrderByDescending(s => s.CreatedAtUtc)
                .ToList();
            return Results.Ok(list);
        });

        group.MapPost("", (CreateSegnalazioneDTO dto) =>
        {
            if (string.IsNullOrWhiteSpace(dto.Titolo))
                return Results.BadRequest("Il titolo è obbligatorio.");

            if (string.IsNullOrWhiteSpace(dto.Descrizione))
                return Results.BadRequest("La descrizione è obbligatoria.");

            var result = SegnalazioniStore.Add(dto);
            return Results.Ok(result);
        });

        group.MapPut("/{id}/stato", (int id, UpdateStatoDTO dto) =>
        {
            if (string.IsNullOrWhiteSpace(dto.Stato))
                return Results.BadRequest("Lo stato è obbligatorio.");

            var result = SegnalazioniStore.UpdateStato(id, dto.Stato);
            return result is null ? Results.NotFound() : Results.Ok(result);
        });
    }
}

/// <summary>
/// Rappresenta il payload per aggiornare lo stato di una segnalazione.
/// </summary>
public class UpdateStatoDTO
{
    public string Stato { get; set; } = string.Empty;
}
