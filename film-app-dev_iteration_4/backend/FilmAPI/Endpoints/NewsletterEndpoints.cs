using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using FilmAPI.DTO;
using FilmAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace FilmAPI.Endpoints;

/// <summary>
/// Raggruppa gli endpoint per iscrizione e gestione newsletter.
/// </summary>
public static class NewsletterEndpoints
{
    // In-memory store for newsletter subscribers (production would use DB)
    private static readonly ConcurrentDictionary<string, NewsletterSubscriber> Subscribers = new(StringComparer.OrdinalIgnoreCase);

    private sealed record NewsletterSubscriber
    {
        public string Email { get; init; } = string.Empty;
        public string? Nome { get; init; }
        public DateTime SubscribedAtUtc { get; init; }
        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// Mappa le rotte newsletter per iscrizione pubblica, disiscrizione e invio notifiche promozionali.
    /// Le rotte pubbliche usano <c>AllowAnonymous</c>; l'invio massivo richiede autorizzazione amministrativa.
    /// Esegue operazioni su iscritti e invii email con effetti sullo store in memoria e sui messaggi inviati.
    /// </summary>
    /// <param name="app">Applicazione web su cui registrare gli endpoint.</param>
    /// <returns>Non restituisce valori.</returns>
    public static void MapNewsletterEndpoints(this WebApplication app)
    {
        // ── Subscribe ────────────────────────────────────────
        app.MapPost("/api/newsletter/subscribe", (
            [FromBody] NewsletterSubscribeDTO dto,
            IEmailService emailService,
            ILogger<Program> logger,
            CancellationToken ct) =>
        {
            // Validate
            if (string.IsNullOrWhiteSpace(dto.Email) || !dto.Email.Contains('@'))
            {
                return Results.BadRequest(new NewsletterResponseDTO
                {
                    Success = false,
                    Message = "Inserisci un indirizzo email valido."
                });
            }

            var email = dto.Email.Trim().ToLowerInvariant();

            // Check if already subscribed
            if (Subscribers.TryGetValue(email, out var existing))
            {
                if (existing.IsActive)
                {
                    return Results.Ok(new NewsletterResponseDTO
                    {
                        Success = true,
                        Message = "Sei già iscritto alla newsletter! 🎬",
                        WelcomeEmailSent = false
                    });
                }

                // Reactivate
                existing.IsActive = true;
                logger.LogInformation("Newsletter: riattivato iscritto {Email}", email);
                return Results.Ok(new NewsletterResponseDTO
                {
                    Success = true,
                    Message = "Iscrizione riattivata con successo! 🎬",
                    WelcomeEmailSent = false
                });
            }

            // New subscriber
            var subscriber = new NewsletterSubscriber
            {
                Email = email,
                Nome = dto.Nome?.Trim(),
                SubscribedAtUtc = DateTime.UtcNow,
                IsActive = true
            };

            Subscribers[email] = subscriber;
            logger.LogInformation("Newsletter: nuovo iscritto {Email}", email);

            // Send welcome email (fire and forget — non blocchiamo la response)
            _ = emailService.SendNewsletterWelcomeAsync(email, subscriber.Nome, ct);

            return Results.Ok(new NewsletterResponseDTO
            {
                Success = true,
                Message = "Iscrizione completata! Riceverai una email di benvenuto. 🎬",
                WelcomeEmailSent = true
            });
        }).AllowAnonymous();

        // ── Unsubscribe ──────────────────────────────────────
        app.MapPost("/api/newsletter/unsubscribe", (
            [FromBody] NewsletterSubscribeDTO dto,
            ILogger<Program> logger) =>
        {
            if (string.IsNullOrWhiteSpace(dto.Email))
            {
                return Results.BadRequest(new NewsletterResponseDTO
                {
                    Success = false,
                    Message = "Inserisci un indirizzo email valido."
                });
            }

            var email = dto.Email.Trim().ToLowerInvariant();

            if (Subscribers.TryGetValue(email, out var subscriber))
            {
                subscriber.IsActive = false;
                logger.LogInformation("Newsletter: disiscritto {Email}", email);
                return Results.Ok(new NewsletterResponseDTO
                {
                    Success = true,
                    Message = "Ti sei disiscritto dalla newsletter. Ci dispiace vederti andare! 😢"
                });
            }

            return Results.Ok(new NewsletterResponseDTO
            {
                Success = true,
                Message = "Email non trovata tra gli iscritti."
            });
        }).AllowAnonymous();

        // ── Admin: send offer notification to all subscribers ─
        app.MapPost("/api/newsletter/notify-offers", async (
            [FromBody] OfferNotificationDTO dto,
            IEmailService emailService,
            ILogger<Program> logger,
            CancellationToken ct) =>
        {
            var activeSubscribers = Subscribers.Values
                .Where(s => s.IsActive)
                .ToList();

            if (activeSubscribers.Count == 0)
            {
                return Results.Ok(new { Sent = 0, Total = 0, Message = "Nessun iscritto attivo." });
            }

            var successCount = 0;
            var offersHtml = dto.HtmlContent ?? string.Empty;

            foreach (var sub in activeSubscribers)
            {
                try
                {
                    var result = await emailService.SendNewOffersNotificationAsync(
                        sub.Email, sub.Nome, offersHtml, ct);
                    if (result.Success) successCount++;
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Errore invio notifica offerte a {Email}", sub.Email);
                }
            }

            logger.LogInformation("Newsletter: inviate {Success}/{Total} notifiche offerte",
                successCount, activeSubscribers.Count);

            return Results.Ok(new
            {
                Sent = successCount,
                Total = activeSubscribers.Count,
                Message = $"Notifiche inviate: {successCount}/{activeSubscribers.Count}"
            });
        }).RequireAuthorization("AdminOnly");

        // ── Admin: get subscriber count ─────────────────────
        app.MapGet("/api/newsletter/stats", () =>
        {
            var active = Subscribers.Values.Count(s => s.IsActive);
            var total = Subscribers.Count;
            return Results.Ok(new { Active = active, Total = total });
        }).RequireAuthorization("AdminOnly");
    }
}

/// <summary>
/// Rappresenta il payload usato per inviare una notifica promozionale newsletter.
/// </summary>
/// <summary>
/// Rappresenta il payload usato per inviare una notifica promozionale newsletter.
/// </summary>
public class OfferNotificationDTO
{
    public string? Subject { get; set; }
    public string? HtmlContent { get; set; }
}
