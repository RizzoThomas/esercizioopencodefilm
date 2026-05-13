using Microsoft.EntityFrameworkCore;
using FilmAPI.Data;
using FilmAPI.Model;
using System.Text;

namespace FilmAPI.Endpoints;

public static class AdminAnalyticsEndpoints
{
    public static void MapAdminAnalyticsEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/admin/analytics").RequireAuthorization("PowerUserOrAdmin");

        group.MapGet("overview", async (FilmDbContext db) =>
        {
            var now = DateTime.UtcNow;
            var thisWeekStart = now.Date.AddDays(-(int)now.DayOfWeek + 1);
            var lastWeekStart = thisWeekStart.AddDays(-7);
            thisWeekStart = DateTime.SpecifyKind(thisWeekStart, DateTimeKind.Utc);
            lastWeekStart = DateTime.SpecifyKind(lastWeekStart, DateTimeKind.Utc);

            var totalUsers = await db.Users.CountAsync();
            var usersThisWeek = await db.Users.CountAsync(u => u.DataRegistrazione >= thisWeekStart);
            var usersLastWeek = await db.Users.CountAsync(u => u.DataRegistrazione >= lastWeekStart && u.DataRegistrazione < thisWeekStart);

            var totalOrdini = await db.Ordini.CountAsync();
            var ordiniThisWeek = await db.Ordini.CountAsync(o => o.CreatedAtUtc >= thisWeekStart);
            var ordiniLastWeek = await db.Ordini.CountAsync(o => o.CreatedAtUtc >= lastWeekStart && o.CreatedAtUtc < thisWeekStart);

            var revenueThisWeek = await db.Ordini
                .Where(o => o.CreatedAtUtc >= thisWeekStart && o.Stato == OrdineState.Paid)
                .SumAsync(o => (decimal?)o.TotaleLordo) ?? 0;

            var revenueLastWeek = await db.Ordini
                .Where(o => o.CreatedAtUtc >= lastWeekStart && o.CreatedAtUtc < thisWeekStart && o.Stato == OrdineState.Paid)
                .SumAsync(o => (decimal?)o.TotaleLordo) ?? 0;

            return Results.Ok(new
            {
                totalUsers, usersThisWeek, usersLastWeek, totalOrdini, ordiniThisWeek, ordiniLastWeek,
                revenueThisWeek = Math.Round(revenueThisWeek, 2),
                revenueLastWeek = Math.Round(revenueLastWeek, 2),
                revenueChange = revenueLastWeek > 0 ? Math.Round((revenueThisWeek - revenueLastWeek) / revenueLastWeek * 100, 1) : 0
            });
        });

        group.MapGet("top-films", async (FilmDbContext db) =>
        {
            var films = await db.Prenotazioni
                .Include(p => p.Proiezione).ThenInclude(pp => pp!.Film)
                .Where(p => p.Proiezione != null && p.Proiezione.Film != null)
                .GroupBy(p => new { p.Proiezione!.FilmId, p.Proiezione.Film!.Titolo })
                .OrderByDescending(g => g.Count()).Take(10)
                .Select(g => new { filmId = g.Key.FilmId, titolo = g.Key.Titolo, prenotazioni = g.Count() })
                .ToListAsync();
            return Results.Ok(films);
        });

        group.MapGet("top-offerte", async (FilmDbContext db) =>
        {
            var subs = await db.UserSubscriptions
                .Include(us => us.Abbonamento)
                .Where(us => us.Abbonamento != null)
                .GroupBy(us => new { us.AbbonamentoId, us.Abbonamento!.Nome })
                .OrderByDescending(g => g.Count()).Take(10)
                .Select(g => new { offertaId = g.Key.AbbonamentoId, titolo = g.Key.Nome, acquisti = g.Count() })
                .ToListAsync();
            return Results.Ok(subs);
        });

        group.MapGet("monthly", async (FilmDbContext db) =>
        {
            var sixMonthsAgo = DateTime.UtcNow.AddMonths(-6);
            var monthly = await db.Ordini
                .Where(o => o.CreatedAtUtc >= sixMonthsAgo && o.Stato == OrdineState.Paid)
                .GroupBy(o => new { o.CreatedAtUtc.Year, o.CreatedAtUtc.Month })
                .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
                .Select(g => new { year = g.Key.Year, month = g.Key.Month, ordini = g.Count(), revenue = Math.Round(g.Sum(o => o.TotaleLordo), 2) })
                .ToListAsync();
            return Results.Ok(monthly);
        });

        // ── CSV Export ─────────────────────────────────────────
        group.MapGet("reports/csv", async (
            FilmDbContext db,
            string? from,
            string? to,
            int? cinemaId) =>
        {
            var fromDate = string.IsNullOrWhiteSpace(from) ? DateTime.UtcNow.Date.AddMonths(-1) : DateTime.Parse(from).ToUniversalTime().Date;
            var toDate = string.IsNullOrWhiteSpace(to) ? DateTime.UtcNow.Date.AddDays(1) : DateTime.Parse(to).ToUniversalTime().Date.AddDays(1);

            var query = db.Ordini
                .Include(o => o.Show!).ThenInclude(s => s!.Film)
                .Include(o => o.Show!).ThenInclude(s => s!.Cinema)
                .Include(o => o.Show!).ThenInclude(s => s!.Sala)
                .Where(o => o.CreatedAtUtc >= fromDate && o.CreatedAtUtc < toDate);

            if (cinemaId.HasValue)
                query = query.Where(o => o.CinemaId == cinemaId.Value);

            var ordini = await query
                .OrderByDescending(o => o.CreatedAtUtc)
                .Select(o => new
                {
                    OrdineId = o.Id,
                    Codice = o.CodiceOrdine,
                    Data = o.CreatedAtUtc,
                    Film = o.Show!.Film!.Titolo,
                    Cinema = o.Show!.Cinema!.Nome,
                    Sala = o.Show!.Sala!.Nome,
                    Biglietti = o.NumeroBiglietti,
                    Totale = o.TotaleLordo,
                    Stato = o.Stato.ToString()
                })
                .ToListAsync();

            var sb = new StringBuilder();
            sb.AppendLine("ID Ordine;Codice;Data;Film;Cinema;Sala;Biglietti;Totale EUR;Stato");
            foreach (var o in ordini)
            {
                sb.AppendLine($"{o.OrdineId};{o.Codice};{o.Data:yyyy-MM-dd HH:mm};\"{o.Film}\";\"{o.Cinema}\";\"{o.Sala}\";{o.Biglietti};{o.Totale:F2};{o.Stato}");
            }

            var csv = sb.ToString();
            var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv)).ToArray();

            return Results.File(bytes, "text/csv; charset=utf-8", $"report-vendite-{DateTime.UtcNow:yyyyMMdd}.csv");
        });

        // ── Alerts: show occupati >= 80% ──────────────────────
        group.MapGet("alerts", async (FilmDbContext db) =>
        {
            var now = DateTime.UtcNow;
            var upcoming = await db.Shows
                .Include(s => s.Film)
                .Include(s => s.Cinema)
                .Include(s => s.Sala)
                .Where(s => s.StartAtUtc >= now && s.StartAtUtc <= now.AddDays(3))
                .ToListAsync();

            var alerts = new List<object>();

            foreach (var show in upcoming)
            {
                var sold = await db.Ordini
                    .CountAsync(o => o.ShowId == show.Id && o.Stato == OrdineState.Paid);

                var capacity = await db.SalaPosti.CountAsync(sp => sp.SalaId == show.SalaId);
                var pct = capacity > 0 ? Math.Round((double)sold / capacity * 100, 1) : 0;

                if (pct >= 80)
                {
                    alerts.Add(new
                    {
                        showId = show.Id,
                        film = show.Film?.Titolo ?? "N/D",
                        cinema = show.Cinema?.Nome ?? "N/D",
                        sala = show.Sala?.Nome ?? "N/D",
                        startAt = show.StartAtUtc,
                        sold,
                        capacity,
                        pct
                    });
                }
            }

            return Results.Ok(alerts.Take(10));
        });
    }
}
