using FilmAPI.Data;
using FilmAPI.Model;
using Microsoft.EntityFrameworkCore;

namespace FilmAPI.Endpoints;

public static class NotificheEndpoints
{
    public static void MapNotificheEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/notifications").RequireAuthorization();

        // ── GET: tutte le notifiche utente (con auto-generazione da dati reali) ──
        group.MapGet("/", async (HttpContext http, FilmDbContext db, CancellationToken ct) =>
        {
            var userId = GetUserId(http);
            if (userId == null) return Results.Unauthorized();

            // 1) Fetch persisted notifications
            var notificheDb = await db.Notifiche
                .Where(n => n.UserId == userId.Value)
                .OrderByDescending(n => n.CreatedAtUtc)
                .Take(20)
                .ToListAsync(ct);

            var result = notificheDb.Select(n => new
            {
                id = n.Id.ToString(),
                icon = n.Icona ?? GetDefaultIcon(n.Tipo),
                title = n.Titolo,
                desc = n.Descrizione ?? "",
                time = FormatRelativeTime(n.CreatedAtUtc),
                createdAt = n.CreatedAtUtc
            }).ToList();

            // 2) Auto-genera notifiche da ordini recenti
            var ordiniRecenti = await db.Ordini
                .Include(o => o.Show!).ThenInclude(s => s!.Film)
                .Where(o => o.UserId == userId.Value && o.Stato == OrdineState.Paid)
                .OrderByDescending(o => o.PaidAtUtc ?? o.CreatedAtUtc)
                .Take(5)
                .ToListAsync(ct);

            foreach (var o in ordiniRecenti)
            {
                var notifId = "ord_" + o.Id;
                if (result.Any(r => r.id == notifId)) continue;

                // Check if already persisted
                var alreadySaved = await db.Notifiche.AnyAsync(n => n.UserId == userId && n.Tipo == "biglietto" && n.Titolo.Contains(o.CodiceOrdine ?? ""), ct);
                if (alreadySaved) { result.Add(new { id = notifId, icon = "fa-solid fa-ticket", title = "Biglietto acquistato", desc = $"Conferma per {o.Show?.Film?.Titolo ?? "film"} — {o.NumeroBiglietti} biglietto/i.", time = FormatRelativeTime(o.PaidAtUtc ?? o.CreatedAtUtc), createdAt = o.PaidAtUtc ?? o.CreatedAtUtc }); continue; }

                // Auto-persist
                db.Notifiche.Add(new Notifica
                {
                    UserId = userId.Value,
                    Tipo = "biglietto",
                    Titolo = "Biglietto acquistato",
                    Descrizione = $"Conferma per {o.Show?.Film?.Titolo ?? "film"} — {o.NumeroBiglietti} biglietto/i.",
                    Icona = "fa-solid fa-ticket",
                    CreatedAtUtc = o.PaidAtUtc ?? o.CreatedAtUtc
                });
                result.Add(new { id = notifId, icon = "fa-solid fa-ticket", title = "Biglietto acquistato", desc = $"Conferma per {o.Show?.Film?.Titolo ?? "film"} — {o.NumeroBiglietti} biglietto/i.", time = FormatRelativeTime(o.PaidAtUtc ?? o.CreatedAtUtc), createdAt = o.PaidAtUtc ?? o.CreatedAtUtc });
            }

            // 3) Auto-genera promemoria per show imminenti
            var now = DateTime.UtcNow;
            var prossimiShow = await db.Ordini
                .Include(o => o.Show!).ThenInclude(s => s!.Film)
                .Include(o => o.Show!).ThenInclude(s => s!.Cinema)
                .Where(o => o.UserId == userId.Value && o.Stato == OrdineState.Paid && o.Show != null && o.Show.StartAtUtc > now && o.Show.StartAtUtc <= now.AddDays(3))
                .OrderBy(o => o.Show!.StartAtUtc)
                .Take(10)
                .ToListAsync(ct);

            foreach (var o in prossimiShow)
            {
                var notifId = "promemoria_" + o.Id;
                if (result.Any(r => r.id == notifId)) continue;
                var showStart = o.Show!.StartAtUtc;
                var oreMancanti = (showStart - now).TotalHours;
                var tempoLabel = oreMancanti < 24 ? $"tra {Math.Round(oreMancanti)} ore" : $"tra {Math.Round(oreMancanti / 24)} giorni";
                result.Add(new { id = notifId, icon = "fa-solid fa-clock", title = "Promemoria spettacolo", desc = $"«{o.Show.Film?.Titolo}» — {o.Show.Cinema?.Nome} — {o.Show.StartAtUtc:dd/MM HH:mm} ({tempoLabel})", time = "ora", createdAt = now });
            }

            // 4) Salva notifiche auto-generate (per averle persistenti al prossimo giro)
            await db.SaveChangesAsync(ct);

            return Results.Ok(result.OrderByDescending(r => r.createdAt).ToList());
        });

        // ── DELETE: elimina notifica ──
        group.MapDelete("/{id}", async (string id, HttpContext http, FilmDbContext db, CancellationToken ct) =>
        {
            var userId = GetUserId(http);
            if (userId == null) return Results.Unauthorized();

            // Parse id: could be DB id (int) or auto-generated (ord_123, promemoria_123)
            if (int.TryParse(id, out var numericId))
            {
                var notif = await db.Notifiche.FirstOrDefaultAsync(n => n.Id == numericId && n.UserId == userId, ct);
                if (notif == null) return Results.NotFound();
                db.Notifiche.Remove(notif);
                await db.SaveChangesAsync(ct);
            }
            // For auto-generated notifications (ord_X, promemoria_X), just mark them
            // by creating a "deleted" tracking entry (we store deletion preference)
            // Or simply acknowledge delete — they'll regenerate if conditions still met
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
