using FilmAPI.Data;
using FilmAPI.Model;
using Microsoft.EntityFrameworkCore;

namespace FilmAPI.Endpoints;

public static class NotificheEndpoints
{
    public static void MapNotificheEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/notifications").RequireAuthorization();

        // ── GET: tutte le notifiche ──
        group.MapGet("/", async (HttpContext http, FilmDbContext db, CancellationToken ct) =>
        {
            var userId = GetUserId(http);
            if (userId == null) return Results.Unauthorized();

            // Get list of deleted source IDs for this user
            var deletedIds = await db.NotificheSoppresse
                .Where(d => d.UserId == userId.Value)
                .Select(d => d.SourceId)
                .ToListAsync(ct);

            var result = new List<object>();
            var toPersist = new List<Notifica>();
            var now = DateTime.UtcNow;

            // 1) Persisted notifications (skip deleted ones)
            var notificheDb = await db.Notifiche
                .Where(n => n.UserId == userId.Value)
                .OrderByDescending(n => n.CreatedAtUtc)
                .Take(20)
                .ToListAsync(ct);

            foreach (var n in notificheDb)
            {
                var sid = "db_" + n.Id;
                if (deletedIds.Contains(sid)) continue;
                result.Add(new { id = sid, icon = n.Icona ?? GetDefaultIcon(n.Tipo), title = n.Titolo, desc = n.Descrizione ?? "", time = FormatRelativeTime(n.CreatedAtUtc), createdAt = n.CreatedAtUtc });
            }

            // 2) Auto-genera da ordini recenti (biglietto acquistato)
            var ordiniRecenti = await db.Ordini
                .Include(o => o.Show!).ThenInclude(s => s!.Film)
                .Where(o => o.UserId == userId.Value && o.Stato == OrdineState.Paid)
                .OrderByDescending(o => o.PaidAtUtc ?? o.CreatedAtUtc)
                .Take(5)
                .ToListAsync(ct);

            foreach (var o in ordiniRecenti)
            {
                var sid = "ord_" + o.Id;
                if (deletedIds.Contains(sid)) continue;
                if (result.Any(r => (string)r.GetType().GetProperty("id")?.GetValue(r, null) == sid)) continue;

                var titoloFilm = o.Show?.Film?.Titolo ?? "film";
                // Persist so it doesn't get lost
                var notif = new Notifica { UserId = userId.Value, Tipo = "biglietto", Titolo = "Biglietto acquistato", Descrizione = $"Conferma per {titoloFilm} — {o.NumeroBiglietti} biglietto/i.", Icona = "fa-solid fa-ticket", CreatedAtUtc = o.PaidAtUtc ?? o.CreatedAtUtc };
                db.Notifiche.Add(notif);
                await db.SaveChangesAsync(ct);
                result.Add(new { id = "db_" + notif.Id, icon = "fa-solid fa-ticket", title = "Biglietto acquistato", desc = $"Conferma per {titoloFilm} — {o.NumeroBiglietti} biglietto/i.", time = FormatRelativeTime(o.PaidAtUtc ?? o.CreatedAtUtc), createdAt = o.PaidAtUtc ?? o.CreatedAtUtc });
            }

            // 3) Auto-genera promemoria per show nelle prossime 72h
            var prossimiShow = await db.Ordini
                .Include(o => o.Show!).ThenInclude(s => s!.Film)
                .Include(o => o.Show!).ThenInclude(s => s!.Cinema)
                .Where(o => o.UserId == userId.Value && o.Stato == OrdineState.Paid && o.Show != null && o.Show.StartAtUtc > now && o.Show.StartAtUtc <= now.AddDays(3))
                .OrderBy(o => o.Show!.StartAtUtc)
                .Take(10)
                .ToListAsync(ct);

            foreach (var o in prossimiShow)
            {
                var sid = "prom_" + o.Id;
                if (deletedIds.Contains(sid)) continue;
                if (result.Any(r => (string)r.GetType().GetProperty("id")?.GetValue(r, null) == sid)) continue;

                var showStart = o.Show!.StartAtUtc;
                var oreMancanti = (int)(showStart - now).TotalHours;
                var tempoLabel = oreMancanti < 24 ? $"tra {oreMancanti}h" : $"tra {oreMancanti / 24}g";
                var notif = new Notifica { UserId = userId.Value, Tipo = "promemoria", Titolo = "Promemoria spettacolo", Descrizione = $"«{o.Show.Film?.Titolo}» — {o.Show.Cinema?.Nome} — {o.Show.StartAtUtc:dd/MM HH:mm} ({tempoLabel})", Icona = "fa-solid fa-clock", CreatedAtUtc = now };
                db.Notifiche.Add(notif);
                await db.SaveChangesAsync(ct);
                result.Add(new { id = "db_" + notif.Id, icon = "fa-solid fa-clock", title = "Promemoria spettacolo", desc = $"«{o.Show.Film?.Titolo}» — {o.Show.Cinema?.Nome} — {o.Show.StartAtUtc:dd/MM HH:mm} ({tempoLabel})", time = "ora", createdAt = now });
            }

            return Results.Ok(result.OrderByDescending(r => {
                var p = r.GetType().GetProperty("createdAt")?.GetValue(r, null);
                return p is DateTime dt ? dt : DateTime.MinValue;
            }).ToList());
        });

        // ── DELETE ──
        group.MapDelete("/{id}", async (string id, HttpContext http, FilmDbContext db, CancellationToken ct) =>
        {
            var userId = GetUserId(http);
            if (userId == null) return Results.Unauthorized();

            if (id.StartsWith("db_") && int.TryParse(id[3..], out var numericId))
            {
                var notif = await db.Notifiche.FirstOrDefaultAsync(n => n.Id == numericId && n.UserId == userId, ct);
                if (notif != null) db.Notifiche.Remove(notif);
            }

            // Track deleted auto-generated source IDs so they never reappear
            db.NotificheSoppresse.Add(new NotificaSoppressa { UserId = userId.Value, SourceId = id });
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { success = true });
        });
    }

    private static string GetDefaultIcon(string tipo) => tipo switch
    {
        "biglietto" => "fa-solid fa-ticket",
        "rimborso" => "fa-solid fa-rotate-left",
        "offerta" => "fa-solid fa-tag",
        "promemoria" => "fa-solid fa-clock",
        "anteprima" => "fa-solid fa-star",
        _ => "fa-solid fa-bell"
    };

    private static string FormatRelativeTime(DateTime utc)
    {
        var diff = DateTime.UtcNow - utc;
        if (diff.TotalMinutes < 1) return "ora";
        if (diff.TotalMinutes < 60) return $"{Math.Round(diff.TotalMinutes)} min fa";
        if (diff.TotalHours < 24) return $"{Math.Round(diff.TotalHours)} h fa";
        if (diff.TotalDays < 7) return $"{Math.Round(diff.TotalDays)} g fa";
        return utc.ToString("dd/MM/yyyy");
    }

    private static int? GetUserId(HttpContext http)
    {
        var claim = http.User.FindFirst("userId")
            ?? http.User.FindFirst("sub")
            ?? http.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        return claim != null && int.TryParse(claim.Value, out var id) ? id : null;
    }
}
