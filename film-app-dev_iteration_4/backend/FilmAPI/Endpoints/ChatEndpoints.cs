using System.Security.Claims;
using FilmAPI.Data;
using FilmAPI.DTO;
using FilmAPI.Model;
using Microsoft.EntityFrameworkCore;

namespace FilmAPI.Endpoints;

public static class ChatEndpoints
{
    private static readonly Dictionary<string, string[]> Faq = new(StringComparer.OrdinalIgnoreCase)
    {
        ["programmazione"] = new[] { "programmazione", "film", "spettacolo", "spettacoli", "orari", "proiezioni", "cinema orari", "uscita" },
        ["biglietti"] = new[] { "biglietto", "biglietti", "acquistare", "comprare", "prezzo", "costo", "pagare", "pagamento", "ticket" },
        ["registrazione"] = new[] { "registrare", "registrazione", "iscrivere", "iscrizione", "creare account", "nuovo utente" },
        ["login"] = new[] { "login", "accedere", "accesso", "entrare", "password", "account bloccato" },
        ["offerte"] = new[] { "offerta", "offerte", "promozione", "promo", "sconto", "abbonamento", "voucher" },
        ["prenotazione"] = new[] { "prenotare", "prenotazione", "prenotazioni", "posto", "posti", "sala", "sale" },
        ["cinema"] = new[] { "cinema", "dove", "indirizzo", "sede", "mappa", "località" },
        ["profilo"] = new[] { "profilo", "modifica", "dati", "email", "nome", "password dimenticata" },
        ["rimborso"] = new[] { "rimborso", "annullare", "cancellare", "disdire" },
    };

    private static readonly Dictionary<string, string> FaqAnswers = new(StringComparer.OrdinalIgnoreCase)
    {
        ["programmazione"] = "🎬 Puoi vedere tutti i film in programmazione nella pagina <a href='/programmazione.html' class='text-ferrari-primary underline'>Programmazione</a>. Scegli il cinema e la data per vedere gli spettacoli disponibili!",
        ["biglietti"] = "🎟️ Puoi acquistare i biglietti dalla pagina di <a href='/programmazione.html' class='text-ferrari-primary underline'>Programmazione</a>. Scegli il film, l'orario e i posti, poi procedi al pagamento. I prezzi variano in base al tipo di sala (2D, 3D, IMAX).",
        ["registrazione"] = "📝 Per registrarti vai alla pagina <a href='/registrazione.html' class='text-ferrari-primary underline'>Registrazione</a>. Inserisci nome, cognome, email e password. Riceverai un'email di conferma.",
        ["login"] = "🔑 Vai alla pagina <a href='/login.html' class='text-ferrari-primary underline'>Login</a> e inserisci email e password. Se hai dimenticato la password, clicca su 'Password dimenticata'.",
        ["offerte"] = "💎 Abbiamo diverse offerte e abbonamenti! Visita la pagina <a href='/offerte.html' class='text-ferrari-primary underline'>Offerte</a> per vedere tutte le promozioni attive, inclusi abbonamenti mensili e pacchetti combo.",
        ["prenotazione"] = "💺 Per prenotare: vai su <a href='/programmazione.html' class='text-ferrari-primary underline'>Programmazione</a>, scegli cinema e film, seleziona l'orario e poi scegli i posti sulla mappa interattiva. Puoi zoomare e selezionare fino a 8 posti!",
        ["cinema"] = "📍 CineBase ha 3 cinema in Italia! Vai su <a href='/my-cinemas.html' class='text-ferrari-primary underline'>I Nostri Cinema</a> per vedere indirizzi, orari e servizi di ogni sede.",
        ["profilo"] = "👤 Nel tuo <a href='/profilo.html' class='text-ferrari-primary underline'>Profilo</a> puoi modificare i dati, vedere le prenotazioni, i biglietti e gestire i tuoi abbonamenti.",
        ["rimborso"] = "🔄 Per richiedere un rimborso o annullare una prenotazione, contatta il supporto. Le condizioni di rimborso variano in base al tipo di biglietto. Usa il pulsante 'Invia Ticket' qui sotto per assistenza.",
    };

    public static void MapChatEndpoints(this WebApplication app)
    {
        // Chat FAQ endpoint — pubblico
        app.MapPost("/api/chat", (ChatRequestDTO request) =>
        {
            var reply = MatchFaq(request.Message, request.FailedAttempts);
            bool showTicket = request.FailedAttempts >= 2 && !reply.IsResolved;
            return Results.Ok(new ChatResponseDTO
            {
                Reply = reply.Reply,
                IsResolved = reply.IsResolved,
                ShowTicketButton = showTicket
            });
        }).AllowAnonymous();

        // Create ticket — pubblico (anche non autenticati)
        app.MapPost("/api/tickets", async (CreateTicketDTO dto, FilmDbContext db, ClaimsPrincipal? user) =>
        {
            if (string.IsNullOrWhiteSpace(dto.Messaggio))
                return Results.BadRequest(new { error = "Il messaggio è obbligatorio." });

            int? userId = null;
            var userIdClaim = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out var uid))
                userId = uid;

            var ticket = new SupportTicket
            {
                UserId = userId,
                Oggetto = string.IsNullOrWhiteSpace(dto.Oggetto) ? "Richiesta supporto" : dto.Oggetto.Trim(),
                Messaggio = dto.Messaggio.Trim(),
                EmailContatto = dto.EmailContatto?.Trim(),
                Stato = TicketStato.Aperto,
                CreatoIl = DateTime.UtcNow
            };

            db.SupportTickets.Add(ticket);
            await db.SaveChangesAsync();

            return Results.Ok(new { message = "Ticket inviato con successo! Ti contatteremo presto.", ticketId = ticket.Id });
        }).AllowAnonymous();

        // Admin: list tickets
        app.MapGet("/admin/tickets", async (FilmDbContext db) =>
        {
            var tickets = await db.SupportTickets
                .Include(t => t.User)
                .OrderByDescending(t => t.CreatoIl)
                .Select(t => new SupportTicketDTO
                {
                    Id = t.Id,
                    UserId = t.UserId,
                    NomeUtente = t.User != null ? $"{t.User.Nome} {t.User.Cognome}" : null,
                    EmailUtente = t.User != null ? t.User.Email : t.EmailContatto,
                    Oggetto = t.Oggetto,
                    Messaggio = t.Messaggio,
                    EmailContatto = t.EmailContatto,
                    Stato = t.Stato.ToString(),
                    CreatoIl = t.CreatoIl,
                    RisoltoIl = t.RisoltoIl
                })
                .ToListAsync();

            return Results.Ok(tickets);
        }).RequireAuthorization("PowerUserOrAdmin");

        // Admin: update ticket status
        app.MapPut("/admin/tickets/{id}/stato", async (int id, TicketStato stato, FilmDbContext db) =>
        {
            var ticket = await db.SupportTickets.FindAsync(id);
            if (ticket == null) return Results.NotFound();

            ticket.Stato = stato;
            if (stato == TicketStato.Risolto || stato == TicketStato.Chiuso)
                ticket.RisoltoIl = DateTime.UtcNow;

            await db.SaveChangesAsync();
            return Results.Ok(new { message = "Stato aggiornato." });
        }).RequireAuthorization("PowerUserOrAdmin");
    }

    private static ChatResponseDTO MatchFaq(string message, int failedAttempts)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return new ChatResponseDTO
            {
                Reply = "Ciao! 👋 Sono l'assistente virtuale di CineBase. Puoi chiedermi informazioni su programmazione, biglietti, registrazione, offerte, prenotazioni e molto altro!",
                IsResolved = true
            };
        }

        var msg = message.ToLowerInvariant();
        string? bestMatch = null;
        int bestScore = 0;

        foreach (var faq in Faq)
        {
            int score = 0;
            foreach (var keyword in faq.Value)
            {
                if (msg.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                    score++;
            }
            if (score > bestScore)
            {
                bestScore = score;
                bestMatch = faq.Key;
            }
        }

        if (bestMatch != null && FaqAnswers.TryGetValue(bestMatch, out var answer))
        {
            return new ChatResponseDTO
            {
                Reply = answer,
                IsResolved = true
            };
        }

        // Saluti
        if (msg.Contains("ciao") || msg.Contains("salve") || msg.Contains("buongiorno") || msg.Contains("hey"))
        {
            return new ChatResponseDTO
            {
                Reply = "Ciao! 👋 Benvenuto in CineBase! Come posso aiutarti? Puoi chiedermi informazioni su film, biglietti, orari, offerte e molto altro.",
                IsResolved = true
            };
        }

        // No match
        return new ChatResponseDTO
        {
            Reply = "Mi dispiace, non ho trovato una risposta per questa domanda. Prova a riformulare o usa il pulsante 'Invia Ticket' per contattare il nostro team di supporto.",
            IsResolved = false
        };
    }
}
