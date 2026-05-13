using System.ComponentModel.DataAnnotations;
using FilmAPI.Data;
using FilmAPI.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FilmAPI.Endpoints;

public static class WatchlistEndpoints
{
    public static void MapWatchlistEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/watchlist").RequireAuthorization();

        // ── GET: Lista film salvati ──────────────────────────
        group.MapGet("/", async (
            HttpContext http,
            FilmDbContext db,
            CancellationToken ct) =>
        {
            var userId = GetUserId(http);
            if (userId == null) return Results.Unauthorized();

            var items = await db.WatchlistItems
                .Include(w => w.Film)
                .ThenInclude(f => f.Regista)
                .Where(w => w.UserId == userId.Value)
                .OrderByDescending(w => w.CreatedAtUtc)
                .Select(w => new WatchlistFilmDTO
                {
                    Id = w.Film!.Id,
                    Titolo = w.Film.Titolo,
                    RegistaNome = w.Film.Regista != null
                        ? $"{w.Film.Regista.Nome} {w.Film.Regista.Cognome}"
                        : null,
                    CopertinaPath = w.Film.CopertinaPath,
                    Anno = w.Film.DataProduzione.Year,
                    DurataMinuti = w.Film.Durata,
                    SalvatoIl = w.CreatedAtUtc
                })
                .ToListAsync(ct);

            return Results.Ok(items);
        });

        // ── GET: Check se un film è salvato ──────────────────
        group.MapGet("/check/{filmId:int}", async (
            int filmId,
            HttpContext http,
            FilmDbContext db,
            CancellationToken ct) =>
        {
            var userId = GetUserId(http);
            if (userId == null) return Results.Unauthorized();

            var saved = await db.WatchlistItems
                .AnyAsync(w => w.UserId == userId.Value && w.FilmId == filmId, ct);

            return Results.Ok(new { isSaved = saved });
        });

        // ── POST: Salva film ─────────────────────────────────
        group.MapPost("/{filmId:int}", async (
            int filmId,
            HttpContext http,
            FilmDbContext db,
            CancellationToken ct) =>
        {
            var userId = GetUserId(http);
            if (userId == null) return Results.Unauthorized();

            var alreadySaved = await db.WatchlistItems
                .AnyAsync(w => w.UserId == userId.Value && w.FilmId == filmId, ct);

            if (alreadySaved)
                return Results.Ok(new { success = true, message = "Film già salvato" });

            var filmExists = await db.Films.AnyAsync(f => f.Id == filmId, ct);
            if (!filmExists)
                return Results.NotFound(new { success = false, message = "Film non trovato" });

            db.WatchlistItems.Add(new WatchlistItem
            {
                UserId = userId.Value,
                FilmId = filmId,
                CreatedAtUtc = DateTime.UtcNow
            });

            await db.SaveChangesAsync(ct);

            return Results.Ok(new { success = true, message = "Film salvato" });
        });

        // ── DELETE: Rimuovi film salvato ─────────────────────
        group.MapDelete("/{filmId:int}", async (
            int filmId,
            HttpContext http,
            FilmDbContext db,
            CancellationToken ct) =>
        {
            var userId = GetUserId(http);
            if (userId == null) return Results.Unauthorized();

            var item = await db.WatchlistItems
                .FirstOrDefaultAsync(w => w.UserId == userId.Value && w.FilmId == filmId, ct);

            if (item == null)
                return Results.NotFound(new { success = false, message = "Film non salvato" });

            db.WatchlistItems.Remove(item);
            await db.SaveChangesAsync(ct);

            return Results.Ok(new { success = true, message = "Film rimosso dalla watchlist" });
        });
    }

    private static int? GetUserId(HttpContext http)
    {
        var userIdClaim = http.User.FindFirst("userId")
            ?? http.User.FindFirst("sub")
            ?? http.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);

        if (userIdClaim == null) return null;
        return int.TryParse(userIdClaim.Value, out var id) ? id : null;
    }
}

public class WatchlistFilmDTO
{
    public int Id { get; set; }
    public string Titolo { get; set; } = string.Empty;
    public string? RegistaNome { get; set; }
    public string? CopertinaPath { get; set; }
    public int? Anno { get; set; }
    public int? DurataMinuti { get; set; }
    public DateTime SalvatoIl { get; set; }
}
