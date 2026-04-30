using FilmAPI.DTO;

namespace FilmAPI.Endpoints;

public static class SegnalazioniEndpoints
{
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

public class UpdateStatoDTO
{
    public string Stato { get; set; } = string.Empty;
}
