using FilmAPI.Data;
using FilmAPI.DTO;
using FilmAPI.Model;
using System.Security.Claims;
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

        abbonamentiGroup.MapPost("/{id}/attiva", async (
            int id,
            AttivaAbbonamentoRequest req,
            ClaimsPrincipal user,
            FilmDbContext db) =>
        {
            var userId = int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            if (userId == 0) return Results.Unauthorized();

            var abbonamento = await db.Abbonamenti.FirstOrDefaultAsync(a => a.Id == id && a.Attivo);
            if (abbonamento is null) return Results.NotFound("Abbonamento non trovato.");

            var existing = await db.UserSubscriptions
                .FirstOrDefaultAsync(s => s.UserId == userId && s.Stato == "attivo");
            if (existing is not null)
                return Results.Conflict("Hai gia un abbonamento attivo.");

            var now = DateTime.UtcNow;
            var dataScadenza = abbonamento.Tipo == "annuale"
                ? now.AddYears(1)
                : now.AddMonths(1);

            var sub = new UserSubscription
            {
                UserId = userId,
                AbbonamentoId = id,
                MetodoPagamento = req.MetodoPagamento ?? "carta",
                AutoRinnovo = req.AutoRinnovo,
                DataInizio = now,
                DataScadenza = dataScadenza,
                Stato = "attivo",
                CreatedAtUtc = now
            };

            db.UserSubscriptions.Add(sub);
            await db.SaveChangesAsync();

            return Results.Ok(new { message = "Abbonamento attivato!", subscriptionId = sub.Id, dataScadenza = sub.DataScadenza });
        }).RequireAuthorization("Authenticated");
    }
}
