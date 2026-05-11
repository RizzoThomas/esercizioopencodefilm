using Microsoft.EntityFrameworkCore;
using FilmAPI.Data;
using FilmAPI.Model;

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
    }
}
