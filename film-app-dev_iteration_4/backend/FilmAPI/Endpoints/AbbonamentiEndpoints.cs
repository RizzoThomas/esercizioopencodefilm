using FilmAPI.Data;
using FilmAPI.DTO;
using FilmAPI.Model;
using FilmAPI.Services;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace FilmAPI.Endpoints;

/// <summary>
/// Raggruppa gli endpoint autenticati per la gestione degli abbonamenti.
/// </summary>
public static class AbbonamentiEndpoints
{
    /// <summary>
    /// Mappa il gruppo <c>/abbonamenti</c> per consultare e gestire gli abbonamenti dell'utente autenticato.
    /// Richiede <c>RequireAuthorization("Authenticated")</c>.
    /// Esegue operazioni di lettura e modifica sugli abbonamenti con effetti sul database e sullo stato sottoscrizione.
    /// </summary>
    /// <param name="app">Applicazione web su cui registrare gli endpoint.</param>
    /// <returns>Non restituisce valori.</returns>
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

        abbonamentiGroup.MapPost("/{id}/stripe-checkout-session", async (
            int id,
            HttpContext httpContext,
            ClaimsPrincipal user,
            FilmDbContext db,
            IStripePaymentGateway stripeGateway) =>
        {
            var userId = int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            if (userId == 0) return Results.Unauthorized();

            var abbonamento = await db.Abbonamenti.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id && a.Attivo);
            if (abbonamento is null) return Results.NotFound("Abbonamento non trovato.");

            var frontendBaseUrl = Environment.GetEnvironmentVariable("FRONTEND_BASE_URL") ?? "http://localhost:5001";
            var successUrl = $"{frontendBaseUrl}/pagamento.html?abbonamentoId={id}&stripe=success&session_id={{CHECKOUT_SESSION_ID}}";
            var cancelUrl = $"{frontendBaseUrl}/pagamento.html?abbonamentoId={id}&stripe=cancelled";

            var session = await stripeGateway.CreateCheckoutSessionAsync(
                new StripeCreateCheckoutSessionRequest
                {
                    OrderId = 0,
                    OrderCode = $"ABBONAMENTO-{id}",
                    UserId = userId,
                    ShowId = 0,
                    Amount = abbonamento.PrezzoAnnuale ?? abbonamento.Prezzo,
                    SuccessUrl = successUrl,
                    CancelUrl = cancelUrl
                },
                httpContext.Request.Headers["Idempotency-Key"].FirstOrDefault());

            return Results.Ok(new
            {
                stripeCheckoutSessionId = session.Id,
                stripeCheckoutUrl = session.Url,
                expiresAtUtc = session.ExpiresAt,
                amount = abbonamento.PrezzoAnnuale ?? abbonamento.Prezzo
            });
        }).RequireAuthorization("Authenticated");
    }
}
