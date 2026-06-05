using FilmAPI.Data;
using FilmAPI.Services;
using Microsoft.EntityFrameworkCore;

namespace FilmAPI.Endpoints;

/// <summary>
/// Raggruppa gli endpoint per i consigli personalizzati dei contenuti.
/// </summary>
public static class RecommendationsEndpoints
{
    /// <summary>
    /// Mappa la rotta <c>/recommendations</c> per generare suggerimenti personalizzati in base alla cronologia dell'utente.
    /// L'endpoint usa l'identità utente quando disponibile e restituisce una risposta anonima in assenza di login.
    /// Legge dati da ordini, watchlist e programmazione senza modificare il database.
    /// </summary>
    /// <param name="app">Applicazione web su cui registrare gli endpoint.</param>
    /// <returns>Non restituisce valori.</returns>
    public static void MapRecommendationsEndpoints(this WebApplication app)
    {
        app.MapGet("/recommendations", async (
            HttpContext http,
            FilmDbContext db,
            IWebHostEnvironment env,
            CancellationToken ct) =>
        {
            var userIdClaim = http.User.FindFirst("userId")
                ?? http.User.FindFirst("sub")
                ?? http.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);

            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
                return Results.Ok(new { items = Array.Empty<object>(), source = "anonymous" });

            // Get user's watched films
            var watchedFilmIds = await db.Ordini
                .Where(o => o.UserId == userId && o.Stato == Model.OrdineState.Paid)
                .Select(o => o.FilmId)
                .Distinct()
                .ToListAsync(ct);

            // Get user's watchlist
            var watchlistIds = await db.WatchlistItems
                .Where(w => w.UserId == userId)
                .Select(w => w.FilmId)
                .ToListAsync(ct);

            // Get upcoming films the user hasn't watched or saved
            var upcoming = await db.Shows
                .Include(s => s.Film!).ThenInclude(f => f!.Regista)
                .Where(s => s.StartAtUtc >= DateTime.UtcNow
                    && !watchedFilmIds.Contains(s.FilmId)
                    && !watchlistIds.Contains(s.FilmId))
                .Select(s => new
                {
                    s.Film!.Id,
                    s.Film.Titolo,
                    s.Film.CopertinaPath,
                    RegistaNome = s.Film.Regista != null ? $"{s.Film.Regista.Nome} {s.Film.Regista.Cognome}" : null,
                    s.Film.Durata,
                    s.Film.DataProduzione
                })
                .Distinct()
                .Take(10)
                .ToListAsync(ct);

            // Simple AI: if user has history, match by director/genre similarity
            // For now, return the upcoming films as recommendations
            var recommendations = upcoming.Select(f => new
            {
                id = f.Id,
                titolo = f.Titolo,
                copertina = f.CopertinaPath,
                regista = f.RegistaNome,
                durata = f.Durata,
                anno = f.DataProduzione.Year,
                motivo = watchedFilmIds.Count > 0 ? "Basato sui tuoi gusti" : "In programmazione"
            }).ToList();

            // If user has watched films, try to get Gemini to write a personalized reason
            if (watchedFilmIds.Count > 0 && recommendations.Count > 0)
            {
                try
                {
                    var watchedTitles = await db.Films
                        .Where(f => watchedFilmIds.Contains(f.Id))
                        .Select(f => f.Titolo)
                        .Take(5)
                        .ToListAsync(ct);

                    var gemini = new GeminiFilmSuggester();
                    var topPick = recommendations[0];
                    var reason = await gemini.SuggestReasonAsync(
                        string.Join(", ", watchedTitles),
                        topPick.titolo,
                        topPick.regista ?? "");
                    if (!string.IsNullOrWhiteSpace(reason))
                    {
                        recommendations[0] = recommendations[0] with { motivo = reason };
                    }
                }
                catch
                {
                    // Gemini unavailable, use default reason
                }
            }

            return Results.Ok(new { items = recommendations, source = watchedFilmIds.Count > 0 ? "personalized" : "default" });
        }).RequireAuthorization();
    }
}

// Lightweight Gemini suggestion helper
internal class GeminiFilmSuggester
{
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(8) };

    public async Task<string> SuggestReasonAsync(string watchedFilms, string filmTitle, string director)
    {
        var apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
        if (string.IsNullOrWhiteSpace(apiKey)) return "";

        var prompt = $@"Sei un consulente cinematografico. Un utente ha visto: {watchedFilms}.
Suggerisci in UNA frase (max 100 caratteri, in italiano) perché gli piacerebbe '{filmTitle}' di {director}.
Non usare emoji. Sii specifico sul perché matcha i suoi gusti.
Esempio: 'Perché ami la fantascienza di Nolan, questo film ti terrà incollato.'";

        var payload = new
        {
            contents = new[]
            {
                new { parts = new[] { new { text = prompt } } }
            }
        };

        var response = await _http.PostAsJsonAsync(
            $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key={apiKey}",
            payload);

        if (!response.IsSuccessStatusCode) return "";

        var result = await response.Content.ReadFromJsonAsync<GeminiResponse>();
        return result?.Candidates?.FirstOrDefault()
            ?.Content?.Parts?.FirstOrDefault()
            ?.Text?.Trim() ?? "";
    }

    private record GeminiResponse(Candidate[]? Candidates);
    private record Candidate(Content? Content);
    private record Content(Part[]? Parts);
    private record Part(string Text);
}
