using FilmAPI.Data;
using Microsoft.EntityFrameworkCore;

namespace FilmAPI.Endpoints;

public static class AbbonamentiEndpoints
{
    public static void MapAbbonamentiEndpoints(this WebApplication app)
    {
        var abbonamentiGroup = app.MapGroup("/abbonamenti");

        abbonamentiGroup.MapGet("/", async (FilmDbContext db) =>
        {
            var abbonamenti = await db.Abbonamenti
                .AsNoTracking()
                .Where(a => a.Attivo)
                .OrderBy(a => a.Tipo)
                .ThenBy(a => a.Prezzo)
                .Select(a => new
                {
                    id = a.Id,
                    nome = a.Nome,
                    descrizione = a.Descrizione,
                    tipo = a.Tipo,
                    prezzo = a.Prezzo,
                    prezzoAnnuale = a.PrezzoAnnuale,
                    scontoPercentuale = a.ScontoPercentuale,
                    numeroBigliettiPerMese = a.NumeroBigliettiPerMese,
                    includePopcornPerMese = a.IncludePopcornPerMese
                })
                .ToListAsync();

            return Results.Ok(abbonamenti);
        });
    }
}
