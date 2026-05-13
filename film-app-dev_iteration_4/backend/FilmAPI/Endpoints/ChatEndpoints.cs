using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using FilmAPI.Data;
using FilmAPI.DTO;
using FilmAPI.Model;
using Microsoft.EntityFrameworkCore;

namespace FilmAPI.Endpoints;

public static class ChatEndpoints
{
    private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(10) };
    private static readonly string? _geminiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
    private static readonly string _geminiUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent";

    private const string SystemPrompt = @"Sei l'assistente virtuale di CineBase, una piattaforma di gestione cinematografica con 3 cinema in Italia (Milano, Roma, Napoli).

IL SITO HA QUESTE PAGINE:
- /index.html — Homepage con film in evidenza e hero cinematico
- /programmazione.html — Programmazione film con scelta cinema, data, orari e acquisto biglietti
- /login.html — Accesso con email/password o Google/Microsoft
- /registrazione.html — Creazione nuovo account
- /offerte.html — Offerte speciali (pacchetti combo) e abbonamenti mensili/annuali
- /my-cinemas.html — I 3 cinema CineBase con indirizzi e servizi
- /profilo.html — Profilo utente, prenotazioni, biglietti, impostazioni
- /scheda-film.html?id=X — Dettaglio di un film specifico
- /acquista.html — Selezione posti e acquisto biglietti
- /pagamento.html — Pagamento con Stripe (carta) o credito
- /tmdb-search.html — Ricerca film nel database TMDB

COME FUNZIONA:
- L'utente sceglie un cinema, una data, un film e un orario
- Seleziona i posti sulla mappa interattiva (max 8)
- Paga con carta di credito o credito prepagato
- Riceve i biglietti via email in PDF
- Offre abbonamenti mensili/annuali con biglietti e popcorn inclusi

REGOLE:
- Rispondi SEMPRE in italiano, in modo breve e utile (max 3-4 frasi)
- Per domande su film/programmazione/biglietti: linka /programmazione.html
- Per domande su account/registrazione: linka /registrazione.html o /login.html
- Per domande su offerte/prezzi: linka /offerte.html
- Per domande su dove sono i cinema: linka /my-cinemas.html
- NON inventare informazioni che non ti ho dato
- Se non sai rispondere, suggerisci di usare il pulsante 'Invia Ticket' per contattare il team di supporto
- Sii amichevole ma professionale, usa emoji con moderazione";

    public static void MapChatEndpoints(this WebApplication app)
    {
        app.MapPost("/api/chat", async (ChatRequestDTO request) =>
        {
            // Try Gemini first if key is available
            if (!string.IsNullOrEmpty(_geminiKey))
            {
                try
                {
                    Console.WriteLine($"[Chat] Calling Gemini...");
                    var geminiReply = await CallGemini(request.Message);
                    if (!string.IsNullOrEmpty(geminiReply))
                    {
                        Console.WriteLine($"[Chat] Gemini OK: {geminiReply[..Math.Min(50, geminiReply.Length)]}...");
                        return Results.Ok(new ChatResponseDTO
                        {
                            Reply = geminiReply,
                            IsResolved = true,
                            ShowTicketButton = false
                        });
                    }
                    Console.WriteLine("[Chat] Gemini returned empty");
                }
                catch (Exception ex) { Console.WriteLine($"[Chat] Gemini error: {ex.Message}"); }
            }
            else { Console.WriteLine("[Chat] No GEMINI_API_KEY, using FAQ"); }

            // Fallback to local FAQ
            var reply = MatchFaq(request.Message, request.FailedAttempts);
            bool showTicket = request.FailedAttempts >= 2 && !reply.IsResolved;
            return Results.Ok(new ChatResponseDTO
            {
                Reply = reply.Reply,
                IsResolved = reply.IsResolved,
                ShowTicketButton = showTicket
            });
        }).AllowAnonymous();

        // Create ticket
        app.MapPost("/api/tickets", async (CreateTicketDTO dto, FilmDbContext db, ClaimsPrincipal? user) =>
        {
            if (string.IsNullOrWhiteSpace(dto.Messaggio))
                return Results.BadRequest(new { error = "Il messaggio è obbligatorio." });

            int? userId = null;
            var userIdClaim = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out var uid)) userId = uid;

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
            var tickets = await db.SupportTickets.Include(t => t.User).OrderByDescending(t => t.CreatoIl)
                .Select(t => new SupportTicketDTO
                {
                    Id = t.Id, UserId = t.UserId,
                    NomeUtente = t.User != null ? $"{t.User.Nome} {t.User.Cognome}" : null,
                    EmailUtente = t.User != null ? t.User.Email : t.EmailContatto,
                    Oggetto = t.Oggetto, Messaggio = t.Messaggio,
                    EmailContatto = t.EmailContatto, Stato = t.Stato.ToString(),
                    CreatoIl = t.CreatoIl, RisoltoIl = t.RisoltoIl
                }).ToListAsync();
            return Results.Ok(tickets);
        }).RequireAuthorization("PowerUserOrAdmin");

        // Admin: update ticket status
        app.MapPut("/admin/tickets/{id}/stato", async (int id, TicketStato stato, FilmDbContext db) =>
        {
            var ticket = await db.SupportTickets.FindAsync(id);
            if (ticket == null) return Results.NotFound();
            ticket.Stato = stato;
            if (stato == TicketStato.Risolto || stato == TicketStato.Chiuso) ticket.RisoltoIl = DateTime.UtcNow;
            await db.SaveChangesAsync();
            return Results.Ok(new { message = "Stato aggiornato." });
        }).RequireAuthorization("PowerUserOrAdmin");
    }

    private static async Task<string?> CallGemini(string userMessage)
    {
        var payload = new
        {
            contents = new[] {
                new { role = "user", parts = new[] { new { text = $"{SystemPrompt}\n\nDomanda dell'utente: {userMessage}" } } }
            },
            generationConfig = new { maxOutputTokens = 300, temperature = 0.7 }
        };

        var json = JsonSerializer.Serialize(payload);
        var httpContent = new StringContent(json, Encoding.UTF8, "application/json");
        var resp = await _http.PostAsync($"{_geminiUrl}?key={_geminiKey}", httpContent);
        Console.WriteLine($"[Chat] Gemini HTTP {resp.StatusCode}");
        
        if (!resp.IsSuccessStatusCode)
        {
            var err = await resp.Content.ReadAsStringAsync();
            Console.WriteLine($"[Chat] Gemini error: {err[..Math.Min(200, err.Length)]}");
            return null;
        }
        
        var result = await resp.Content.ReadAsStringAsync();
        Console.WriteLine($"[Chat] Gemini raw: {(result.Length > 200 ? result[..200] : result)}");
        
        using var doc = JsonDocument.Parse(result);
        var root = doc.RootElement;
        
        if (root.TryGetProperty("candidates", out var candidates) && 
            candidates.GetArrayLength() > 0 &&
            candidates[0].TryGetProperty("content", out var msgContent) &&
            msgContent.TryGetProperty("parts", out var parts) &&
            parts.GetArrayLength() > 0 &&
            parts[0].TryGetProperty("text", out var textEl))
        {
            return textEl.GetString()?.Trim();
        }
        
        Console.WriteLine("[Chat] Gemini: unexpected response structure");
        return null;
    }

    private static readonly Dictionary<string, string[]> Faq = new(StringComparer.OrdinalIgnoreCase)
    {
        ["programmazione"] = new[] { "programmazion", "film", "spettacol", "orari", "proiezion", "uscit", "quando" },
        ["biglietti"] = new[] { "bigli", "acquist", "compr", "prezz", "cost", "pagar", "pagament", "ticket", "comprare", "blgli", "bilgl" },
        ["registrazione"] = new[] { "registr", "iscriv", "creare account", "account nuov", "registrazion" },
        ["login"] = new[] { "acced", "access", "entrar", "login", "password" },
        ["offerte"] = new[] { "offert", "promozion", "scont", "abbonament", "voucher", "promo" },
        ["prenotazione"] = new[] { "prenot", "post", "sal", "prenotazion" },
        ["cinema"] = new[] { "cinema", "dove", "indirizz", "sed", "località", "localita" },
        ["profilo"] = new[] { "profil", "modific", "dat", "email", "nome" },
        ["rimborso"] = new[] { "rimbors", "annull", "cancell", "disdir" },
    };

    private static readonly Dictionary<string, string> FaqAnswers = new(StringComparer.OrdinalIgnoreCase)
    {
        ["programmazione"] = "🎬 Vai alla pagina <a href='/programmazione.html' class='text-ferrari-primary underline'>Programmazione</a> per vedere tutti i film e gli orari disponibili.",
        ["biglietti"] = "🎟️ Acquista i biglietti dalla <a href='/programmazione.html' class='text-ferrari-primary underline'>Programmazione</a>: scegli film, orario e posti, poi paga.",
        ["registrazione"] = "📝 Registrati su <a href='/registrazione.html' class='text-ferrari-primary underline'>Registrazione</a> con nome, cognome, email e password.",
        ["login"] = "🔑 Accedi su <a href='/login.html' class='text-ferrari-primary underline'>Login</a> con email e password. Password dimenticata? Clicca sul link dedicato.",
        ["offerte"] = "💎 Scopri offerte e abbonamenti su <a href='/offerte.html' class='text-ferrari-primary underline'>Offerte</a>!",
        ["prenotazione"] = "💺 Prenota dalla <a href='/programmazione.html' class='text-ferrari-primary underline'>Programmazione</a>: scegli cinema, film, orario e posti.",
        ["cinema"] = "📍 CineBase ha 3 cinema in Italia! Vedi <a href='/my-cinemas.html' class='text-ferrari-primary underline'>I Nostri Cinema</a>.",
        ["profilo"] = "👤 Gestisci i tuoi dati dal <a href='/profilo.html' class='text-ferrari-primary underline'>Profilo</a>.",
        ["rimborso"] = "🔄 Per rimborsi o annullamenti, usa 'Invia Ticket' qui sotto per assistenza.",
    };

    private static ChatResponseDTO MatchFaq(string message, int failedAttempts)
    {
        if (string.IsNullOrWhiteSpace(message))
            return new() { Reply = "Ciao! 👋 Sono l'assistente di CineBase. Chiedimi informazioni su film, biglietti, orari e molto altro!", IsResolved = true };

        var msg = message.ToLowerInvariant();
        string? bestMatch = null;
        int bestScore = 0;
        foreach (var faq in Faq)
        {
            int score = faq.Value.Count(k => msg.Contains(k, StringComparison.OrdinalIgnoreCase));
            if (score > bestScore) { bestScore = score; bestMatch = faq.Key; }
        }
        if (bestMatch != null && FaqAnswers.TryGetValue(bestMatch, out var answer))
            return new() { Reply = answer, IsResolved = true };

        if (msg.Contains("ciao") || msg.Contains("salve") || msg.Contains("buongiorno") || msg.Contains("hey"))
            return new() { Reply = "Ciao! 👋 Come posso aiutarti? Chiedimi di programmazione, biglietti, offerte o altro!", IsResolved = true };

        return new() { Reply = "Mi dispiace, non ho trovato una risposta. Prova a riformulare o usa 'Invia Ticket' per il supporto.", IsResolved = false };
    }
}
