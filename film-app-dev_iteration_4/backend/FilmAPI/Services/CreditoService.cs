using FilmAPI.Data;
using FilmAPI.DTO;
using FilmAPI.Model;
using Microsoft.EntityFrameworkCore;

namespace FilmAPI.Services;

public interface ICreditoService
{
    Task<CreditoMeDTO?> GetCreditoMeAsync(int userId);
    Task<List<CreditoUserLookupDTO>> SearchUsersAsync(string? email);
    Task<List<MovimentoCreditoDTO>> GetTopUpsAsync(string? email);
    Task<CreditoTopUpResultDTO> TopUpAsync(int operatorUserId, CreditoTopUpRequestDTO dto);
    Task<MovimentoCredito> ApplyOrderDebitAsync(int userId, int orderId, decimal importo, string? note = null);
    Task<MovimentoCredito> ReserveOrderCreditAsync(int userId, int orderId, decimal importo, string? note = null);
    Task<MovimentoCredito?> ReleaseReservedOrderCreditAsync(int userId, int orderId, string? note = null);
    Task<CreateTopupSessionResponseDTO> CreateTopupSessionAsync(int userId, decimal amount);
    Task<ReconcileTopupResponseDTO> ReconcileTopupSessionAsync(int userId, string sessionId);
}

/// <summary>
/// Fornisce il servizio  per le operazioni di dominio esposte da questo modulo.
/// </summary>
/// <remarks>
/// Usato dai controller o endpoint che gestiscono le funzioni di . Dipendenze iniettate nel costruttore: nessuna dichiarata esplicitamente.
/// </remarks>
public class CreditoService : ICreditoService
{
    private readonly FilmDbContext _db;
    private readonly IStripePaymentGateway _stripeGateway;
    private readonly IEmailService _emailService;
    private readonly ILogger<CreditoService> _logger;

    /// <summary>
    /// Esegue l''operazione CreditoService del servizio.
    /// </summary>
    /// <param name="db">Parametro necessario per l'operazione: db.</param>
    /// <param name="stripeGateway">Parametro necessario per l'operazione: stripeGateway.</param>
    /// <param name="emailService">Parametro necessario per l'operazione: emailService.</param>
    /// <param name="logger">Parametro necessario per l'operazione: logger.</param>
    /// <returns>Restituisce il risultato dell'operazione quando questa ha esito positivo; altrimenti il chiamante riceve un'eccezione o un risultato nullo/booleano secondo il contratto del metodo.</returns>
    /// <remarks>
    /// Effetti collaterali: scrive o aggiorna il database. può inviare email di notifica.
    /// </remarks>
    public CreditoService(FilmDbContext db, IStripePaymentGateway stripeGateway, IEmailService emailService, ILogger<CreditoService> logger)
    {
        _db = db;
        _stripeGateway = stripeGateway;
        _emailService = emailService;
        _logger = logger;
    }

    /// <summary>
    /// Recupera o legge i dati tramite l''operazione GetCreditoMeAsync del servizio.
    /// </summary>
    /// <param name="userId">Identificativo necessario per individuare l'entità o il contesto di lavoro: userId.</param>
    /// <returns>Restituisce in modo asincrono il risultato dell'operazione indicato dal tipo interno del Task quando la logica termina correttamente.</returns>
    /// <remarks>
    /// Effetti collaterali: scrive o aggiorna il database. può inviare email di notifica.
    /// </remarks>
    public async Task<CreditoMeDTO?> GetCreditoMeAsync(int userId)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user is null)
            return null;

        var movimenti = await _db.MovimentiCredito
            .Include(m => m.User)
            .Include(m => m.OperatoreUser)
            .Include(m => m.Cinema)
            .Include(m => m.Ordine!.Show!.Film)
            .Where(m => m.UserId == userId)
            .OrderByDescending(m => m.CreatedAtUtc)
            .Take(50)
            .ToListAsync();

        return new CreditoMeDTO
        {
            UserId = user.Id,
            SaldoAttuale = user.CreditoResiduo,
            Movimenti = movimenti.Select(MapMovimento).ToList()
        };
    }

    /// <summary>
    /// Esegue l''operazione SearchUsersAsync del servizio.
    /// </summary>
    /// <param name="email">Indirizzo email usato per autenticazione, notifica o identificazione dell'utente.</param>
    /// <returns>Restituisce in modo asincrono il risultato dell'operazione indicato dal tipo interno del Task quando la logica termina correttamente.</returns>
    /// <remarks>
    /// Effetti collaterali: scrive o aggiorna il database. può inviare email di notifica.
    /// </remarks>
    public async Task<List<CreditoUserLookupDTO>> SearchUsersAsync(string? email)
    {
        var query = _db.Users.AsQueryable();
        if (!string.IsNullOrWhiteSpace(email))
        {
            var normalized = email.Trim().ToLowerInvariant();
            query = query.Where(u => u.Email.ToLower().Contains(normalized));
        }

        return await query
            .OrderBy(u => u.Email)
            .Take(20)
            .Select(u => new CreditoUserLookupDTO
            {
                Id = u.Id,
                Email = u.Email,
                Nome = u.Nome,
                Cognome = u.Cognome,
                CreditoResiduo = u.CreditoResiduo
            })
            .ToListAsync();
    }

    /// <summary>
    /// Recupera o legge i dati tramite l''operazione GetTopUpsAsync del servizio.
    /// </summary>
    /// <param name="email">Indirizzo email usato per autenticazione, notifica o identificazione dell'utente.</param>
    /// <returns>Restituisce in modo asincrono il risultato dell'operazione indicato dal tipo interno del Task quando la logica termina correttamente.</returns>
    /// <remarks>
    /// Effetti collaterali: scrive o aggiorna il database. può inviare email di notifica.
    /// </remarks>
    public async Task<List<MovimentoCreditoDTO>> GetTopUpsAsync(string? email)
    {
        var query = _db.MovimentiCredito
            .Include(m => m.User)
            .Include(m => m.OperatoreUser)
            .Include(m => m.Cinema)
            .Include(m => m.Ordine)
            .Where(m => m.Tipo == MovimentoCreditoTipo.TopUp)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(email))
        {
            var normalized = email.Trim().ToLowerInvariant();
            query = query.Where(m => m.User != null && m.User.Email.ToLower().Contains(normalized));
        }

        var movimenti = await query
            .OrderByDescending(m => m.CreatedAtUtc)
            .Take(100)
            .ToListAsync();

        return movimenti.Select(MapMovimento).ToList();
    }

    /// <summary>
    /// Esegue l''operazione TopUpAsync del servizio.
    /// </summary>
    /// <param name="operatorUserId">Identificativo necessario per individuare l'entità o il contesto di lavoro: operatorUserId.</param>
    /// <param name="dto">Oggetto DTO di input necessario per eseguire l'operazione.</param>
    /// <returns>Restituisce in modo asincrono il risultato dell'operazione indicato dal tipo interno del Task quando la logica termina correttamente.</returns>
    /// <remarks>
    /// Effetti collaterali: scrive o aggiorna il database. può inviare email di notifica.
    /// </remarks>
    public async Task<CreditoTopUpResultDTO> TopUpAsync(int operatorUserId, CreditoTopUpRequestDTO dto)
    {
        if (dto.Importo <= 0)
            throw new ArgumentException("L'importo della ricarica deve essere maggiore di zero.");

        var operatore = await _db.Users.FindAsync(operatorUserId);
        if (operatore is null)
            throw new InvalidOperationException("Operatore non trovato.");

        var user = await _db.Users.FindAsync(dto.UserId);
        if (user is null)
            throw new InvalidOperationException("Utente destinatario non trovato.");

        if (dto.CinemaId.HasValue)
        {
            var cinemaExists = await _db.Cinemas.AnyAsync(c => c.Id == dto.CinemaId.Value);
            if (!cinemaExists)
                throw new ArgumentException("Cinema non trovato.");
        }

        var now = DateTime.UtcNow;
        var saldoPre = user.CreditoResiduo;
        var saldoPost = saldoPre + dto.Importo;

        user.CreditoResiduo = saldoPost;

        var movimento = new MovimentoCredito
        {
            UserId = user.Id,
            Tipo = MovimentoCreditoTipo.TopUp,
            Importo = dto.Importo,
            SaldoPre = saldoPre,
            SaldoPost = saldoPost,
            OperatoreUserId = operatore.Id,
            CinemaId = dto.CinemaId,
            CreatedAtUtc = now,
            Note = string.IsNullOrWhiteSpace(dto.Note) ? null : dto.Note.Trim()
        };

        _db.MovimentiCredito.Add(movimento);
        await _db.SaveChangesAsync();

        movimento = await _db.MovimentiCredito
            .Include(m => m.User)
            .Include(m => m.OperatoreUser)
            .Include(m => m.Cinema)
            .Include(m => m.Ordine)
            .FirstAsync(m => m.Id == movimento.Id);

        return new CreditoTopUpResultDTO
        {
            Utente = new CreditoUserLookupDTO
            {
                Id = user.Id,
                Email = user.Email,
                Nome = user.Nome,
                Cognome = user.Cognome,
                CreditoResiduo = user.CreditoResiduo
            },
            Movimento = MapMovimento(movimento)
        };
    }

    /// <summary>
    /// Esegue l''operazione ApplyOrderDebitAsync del servizio.
    /// </summary>
    /// <param name="userId">Identificativo necessario per individuare l'entità o il contesto di lavoro: userId.</param>
    /// <param name="orderId">Identificativo necessario per individuare l'entità o il contesto di lavoro: orderId.</param>
    /// <param name="importo">Parametro necessario per l'operazione: importo.</param>
    /// <param name="note">Parametro necessario per l'operazione: note.</param>
    /// <returns>Restituisce in modo asincrono il risultato dell'operazione indicato dal tipo interno del Task quando la logica termina correttamente.</returns>
    /// <remarks>
    /// Effetti collaterali: scrive o aggiorna il database.
    /// </remarks>
    public async Task<MovimentoCredito> ApplyOrderDebitAsync(int userId, int orderId, decimal importo, string? note = null)
    {
        if (importo <= 0)
            throw new ArgumentException("L'importo da addebitare deve essere maggiore di zero.");

        var existing = await _db.MovimentiCredito
            .FirstOrDefaultAsync(m => m.UserId == userId && m.OrdineId == orderId && m.Tipo == MovimentoCreditoTipo.DebitOrder);

        if (existing is not null)
            return existing;

        var user = await _db.Users.FindAsync(userId);
        if (user is null)
            throw new InvalidOperationException("Utente non trovato.");

        if (user.CreditoResiduo < importo)
            throw new InvalidOperationException("Credito insufficiente per completare il pagamento.");

        var saldoPre = user.CreditoResiduo;
        var saldoPost = saldoPre - importo;
        var movimento = new MovimentoCredito
        {
            UserId = userId,
            Tipo = MovimentoCreditoTipo.DebitOrder,
            Importo = -importo,
            SaldoPre = saldoPre,
            SaldoPost = saldoPost,
            OrdineId = orderId,
            CreatedAtUtc = DateTime.UtcNow,
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim()
        };

        user.CreditoResiduo = saldoPost;
        _db.MovimentiCredito.Add(movimento);
        await _db.SaveChangesAsync();
        return movimento;
    }

    /// <summary>
    /// Esegue l''operazione ReserveOrderCreditAsync del servizio.
    /// </summary>
    /// <param name="userId">Identificativo necessario per individuare l'entità o il contesto di lavoro: userId.</param>
    /// <param name="orderId">Identificativo necessario per individuare l'entità o il contesto di lavoro: orderId.</param>
    /// <param name="importo">Parametro necessario per l'operazione: importo.</param>
    /// <param name="note">Parametro necessario per l'operazione: note.</param>
    /// <returns>Restituisce in modo asincrono il risultato dell'operazione indicato dal tipo interno del Task quando la logica termina correttamente.</returns>
    /// <remarks>
    /// Effetti collaterali: scrive o aggiorna il database. può inviare email di notifica.
    /// </remarks>
    public async Task<MovimentoCredito> ReserveOrderCreditAsync(int userId, int orderId, decimal importo, string? note = null)
    {
        if (importo <= 0)
            throw new ArgumentException("L'importo da riservare deve essere maggiore di zero.");

        var existing = await _db.MovimentiCredito
            .FirstOrDefaultAsync(m => m.UserId == userId && m.OrdineId == orderId && m.Tipo == MovimentoCreditoTipo.Adjustment && m.Note != null && m.Note.StartsWith("RESERVE:"));

        if (existing is not null)
            return existing;

        var user = await _db.Users.FindAsync(userId);
        if (user is null)
            throw new InvalidOperationException("Utente non trovato.");

        if (user.CreditoResiduo < importo)
            throw new InvalidOperationException("Credito insufficiente per riservare l'importo richiesto.");

        var saldoPre = user.CreditoResiduo;
        var saldoPost = saldoPre - importo;
        var movimento = new MovimentoCredito
        {
            UserId = userId,
            Tipo = MovimentoCreditoTipo.Adjustment,
            Importo = -importo,
            SaldoPre = saldoPre,
            SaldoPost = saldoPost,
            OrdineId = orderId,
            CreatedAtUtc = DateTime.UtcNow,
            Note = $"RESERVE:{(string.IsNullOrWhiteSpace(note) ? "Riserva credito checkout hosted" : note.Trim())}"
        };

        user.CreditoResiduo = saldoPost;
        _db.MovimentiCredito.Add(movimento);
        await _db.SaveChangesAsync();
        return movimento;
    }

    /// <summary>
    /// Esegue l''operazione ReleaseReservedOrderCreditAsync del servizio.
    /// </summary>
    /// <param name="userId">Identificativo necessario per individuare l'entità o il contesto di lavoro: userId.</param>
    /// <param name="orderId">Identificativo necessario per individuare l'entità o il contesto di lavoro: orderId.</param>
    /// <param name="note">Parametro necessario per l'operazione: note.</param>
    /// <returns>Restituisce in modo asincrono il risultato dell'operazione indicato dal tipo interno del Task quando la logica termina correttamente.</returns>
    /// <remarks>
    /// Effetti collaterali: scrive o aggiorna il database. può inviare email di notifica.
    /// </remarks>
    public async Task<MovimentoCredito?> ReleaseReservedOrderCreditAsync(int userId, int orderId, string? note = null)
    {
        var reserveMovement = await _db.MovimentiCredito
            .FirstOrDefaultAsync(m => m.UserId == userId && m.OrdineId == orderId && m.Tipo == MovimentoCreditoTipo.Adjustment && m.Note != null && m.Note.StartsWith("RESERVE:"));

        if (reserveMovement is null)
            return null;

        var alreadyReleased = await _db.MovimentiCredito
            .AnyAsync(m => m.UserId == userId && m.OrdineId == orderId && m.Tipo == MovimentoCreditoTipo.Refund && m.Note != null && m.Note.StartsWith("RELEASE:"));

        if (alreadyReleased)
            return null;

        var user = await _db.Users.FindAsync(userId);
        if (user is null)
            throw new InvalidOperationException("Utente non trovato.");

        var importo = Math.Abs(reserveMovement.Importo);
        var saldoPre = user.CreditoResiduo;
        var saldoPost = saldoPre + importo;
        var movimento = new MovimentoCredito
        {
            UserId = userId,
            Tipo = MovimentoCreditoTipo.Refund,
            Importo = importo,
            SaldoPre = saldoPre,
            SaldoPost = saldoPost,
            OrdineId = orderId,
            CreatedAtUtc = DateTime.UtcNow,
            Note = $"RELEASE:{(string.IsNullOrWhiteSpace(note) ? "Rilascio credito riservato checkout hosted" : note.Trim())}"
        };

        user.CreditoResiduo = saldoPost;
        _db.MovimentiCredito.Add(movimento);
        await _db.SaveChangesAsync();
        return movimento;
    }

    private static MovimentoCreditoDTO MapMovimento(MovimentoCredito movimento)
    {
        return new MovimentoCreditoDTO
        {
            Id = movimento.Id,
            UserId = movimento.UserId,
            UserEmail = movimento.User?.Email ?? string.Empty,
            Tipo = movimento.Tipo.ToString(),
            Importo = movimento.Importo,
            SaldoPre = movimento.SaldoPre,
            SaldoPost = movimento.SaldoPost,
            OperatoreUserId = movimento.OperatoreUserId,
            OperatoreEmail = movimento.OperatoreUser?.Email,
            CinemaId = movimento.CinemaId,
            CinemaNome = movimento.Cinema?.Nome,
            OrdineId = movimento.OrdineId,
            CodiceOrdine = movimento.Ordine?.CodiceOrdine,
            FilmTitolo = movimento.Ordine?.Show?.Film?.Titolo,
            ShowStartAtUtc = movimento.Ordine?.Show?.StartAtUtc,
            CreatedAtUtc = movimento.CreatedAtUtc,
            Note = movimento.Note
        };
    }

    /// <summary>
    /// Esegue l''operazione di business CreateTopupSessionAsync del servizio.
    /// </summary>
    /// <param name="userId">Identificativo necessario per individuare l'entità o il contesto di lavoro: userId.</param>
    /// <param name="amount">Parametro necessario per l'operazione: amount.</param>
    /// <returns>Restituisce in modo asincrono il risultato dell'operazione indicato dal tipo interno del Task quando la logica termina correttamente.</returns>
    /// <remarks>
    /// Effetti collaterali: può inviare email di notifica. avvia lavoro asincrono in background.
    /// </remarks>
    public async Task<CreateTopupSessionResponseDTO> CreateTopupSessionAsync(int userId, decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentException("L'importo della ricarica deve essere maggiore di zero.");

        if (amount < 1m)
            throw new ArgumentException("L'importo minimo della ricarica è €1,00.");

        if (amount > 500m)
            throw new ArgumentException("L'importo massimo della ricarica è €500,00.");

        var user = await _db.Users.FindAsync(userId);
        if (user is null)
            throw new InvalidOperationException("Utente non trovato.");

        var frontendBaseUrl = Environment.GetEnvironmentVariable("FRONTEND_BASE_URL") ?? "http://localhost:5001";
        var successUrl = $"{frontendBaseUrl}/profilo.html?topup=success&session_id={{CHECKOUT_SESSION_ID}}";
        var cancelUrl = $"{frontendBaseUrl}/profilo.html?topup=cancelled";

        var session = await _stripeGateway.CreateCheckoutSessionAsync(
            new StripeCreateCheckoutSessionRequest
            {
                OrderId = 0,
                OrderCode = $"TOPUP-{userId}-{DateTime.UtcNow:yyyyMMddHHmmss}",
                UserId = userId,
                ShowId = 0,
                Amount = amount,
                SuccessUrl = successUrl,
                CancelUrl = cancelUrl
            },
            $"topup-{userId}-{DateTime.UtcNow:O}");

        return new CreateTopupSessionResponseDTO
        {
            StripeCheckoutSessionId = session.Id,
            StripeCheckoutUrl = session.Url,
            Amount = amount,
            ExpiresAtUtc = session.ExpiresAt
        };
    }

    /// <summary>
    /// Esegue l''operazione ReconcileTopupSessionAsync del servizio.
    /// </summary>
    /// <param name="userId">Identificativo necessario per individuare l'entità o il contesto di lavoro: userId.</param>
    /// <param name="sessionId">Identificativo necessario per individuare l'entità o il contesto di lavoro: sessionId.</param>
    /// <returns>Restituisce in modo asincrono il risultato dell'operazione indicato dal tipo interno del Task quando la logica termina correttamente.</returns>
    /// <remarks>
    /// Effetti collaterali: può inviare email di notifica. avvia lavoro asincrono in background.
    /// </remarks>
    public async Task<ReconcileTopupResponseDTO> ReconcileTopupSessionAsync(int userId, string sessionId)
    {
        var session = await _stripeGateway.GetCheckoutSessionAsync(sessionId);

        if (session.Status != "complete")
        {
            return new ReconcileTopupResponseDTO
            {
                Success = false,
                Message = $"Stato sessione: {session.Status}. Ricarica non ancora completata."
            };
        }

        var existingTopup = await _db.MovimentiCredito
            .AnyAsync(m => m.UserId == userId && m.Tipo == MovimentoCreditoTipo.TopUp
                && m.Note != null && m.Note.Contains(sessionId));

        if (existingTopup)
        {
            var user = await _db.Users.FindAsync(userId);
            return new ReconcileTopupResponseDTO
            {
                Success = true,
                NewBalance = user?.CreditoResiduo ?? 0,
                Message = "Ricarica già elaborata."
            };
        }

        var amount = session.Metadata.TryGetValue("topupAmount", out var amountStr) && decimal.TryParse(amountStr, out var parsedAmount)
            ? parsedAmount
            : session.AmountTotal / 100m;

        var topupResult = await TopUpAsync(userId, new CreditoTopUpRequestDTO
        {
            UserId = userId,
            Importo = amount,
            Note = $"Topup Stripe - Session {sessionId}"
        });

        // Send confirmation email (fire and forget, don't block the response)
        _ = Task.Run(async () =>
        {
            try
            {
                var user = await _db.Users.FindAsync(userId);
                if (user != null && !string.IsNullOrWhiteSpace(user.Email))
                {
                    var recipientName = $"{user.Nome} {user.Cognome}".Trim();
                    if (string.IsNullOrWhiteSpace(recipientName))
                        recipientName = user.Email;

                    await _emailService.SendTopupConfirmationAsync(
                        user.Email,
                        recipientName,
                        amount,
                        topupResult.Utente.CreditoResiduo,
                        sessionId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Errore invio email conferma ricarica per utente {UserId}", userId);
            }
        });

        return new ReconcileTopupResponseDTO
        {
            Success = true,
            NewBalance = topupResult.Utente.CreditoResiduo,
            Message = "Ricarica completata con successo."
        };
    }
}
