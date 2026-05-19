using FilmAPI.Data;
using FilmAPI.DTO;
using FilmAPI.Model;
using Microsoft.EntityFrameworkCore;

namespace FilmAPI.Services;

/// <summary>
/// Fornisce il servizio  per le operazioni di dominio esposte da questo modulo.
/// </summary>
/// <remarks>
/// Usato dai controller o endpoint che gestiscono le funzioni di . Dipendenze iniettate nel costruttore: nessuna dichiarata esplicitamente.
/// </remarks>
public class ValidazioneBigliettoService : IValidazioneBigliettoService
{
    private readonly FilmDbContext _db;
    private readonly IBigliettoService _bigliettoService;

    /// <summary>
    /// Esegue l''operazione ValidazioneBigliettoService del servizio.
    /// </summary>
    /// <param name="db">Parametro necessario per l'operazione: db.</param>
    /// <param name="bigliettoService">Parametro necessario per l'operazione: bigliettoService.</param>
    /// <returns>Restituisce il risultato dell'operazione quando questa ha esito positivo; altrimenti il chiamante riceve un'eccezione o un risultato nullo/booleano secondo il contratto del metodo.</returns>
    /// <remarks>
    /// Effetti collaterali: scrive o aggiorna il database.
    /// </remarks>
    public ValidazioneBigliettoService(FilmDbContext db, IBigliettoService bigliettoService)
    {
        _db = db;
        _bigliettoService = bigliettoService;
    }

    /// <summary>
    /// Recupera o legge i dati tramite l''operazione GetTicketByCodeAsync del servizio.
    /// </summary>
    /// <param name="code">Parametro necessario per l'operazione: code.</param>
    /// <returns>Restituisce in modo asincrono il risultato dell'operazione indicato dal tipo interno del Task quando la logica termina correttamente.</returns>
    /// <remarks>
    /// Effetti collaterali: scrive o aggiorna il database.
    /// </remarks>
    public Task<TicketValidationLookupDTO?> GetTicketByCodeAsync(string code)
    {
        return _bigliettoService.GetTicketValidationLookupAsync(code);
    }

    /// <summary>
    /// Esegue l''operazione di business ValidateAsync del servizio.
    /// </summary>
    /// <param name="operatorUserId">Identificativo necessario per individuare l'entità o il contesto di lavoro: operatorUserId.</param>
    /// <param name="request">Parametro necessario per l'operazione: request.</param>
    /// <returns>Restituisce in modo asincrono il risultato dell'operazione indicato dal tipo interno del Task quando la logica termina correttamente.</returns>
    /// <remarks>
    /// Effetti collaterali: scrive o aggiorna il database.
    /// </remarks>
    public async Task<TicketValidationResultDTO> ValidateAsync(int operatorUserId, TicketValidationRequestDTO request)
    {
        if (operatorUserId <= 0)
            throw new ArgumentException("Operatore non valido.");

        if (request.CinemaId <= 0)
            throw new ArgumentException("CinemaId non valido.");

        var normalizedCode = NormalizeCode(request.CodiceBiglietto);

        await using var transaction = await _db.Database.BeginTransactionAsync();

        var ticket = await _db.Biglietti
            .Include(b => b.Ordine)
            .Include(b => b.Show)
            .FirstOrDefaultAsync(b => b.CodiceBiglietto.ToUpper() == normalizedCode);

        if (ticket is null)
            throw new KeyNotFoundException("Biglietto non trovato.");

        var ticketCinemaId = ticket.Show?.CinemaId ?? ticket.Ordine?.CinemaId ?? 0;
        if (ticketCinemaId != request.CinemaId)
            throw new InvalidOperationException("Il biglietto appartiene a un cinema diverso da quello operativo selezionato.");

        if (ticket.Stato == BigliettoState.Cancelled)
            throw new InvalidOperationException("Il biglietto risulta annullato e non puo essere validato.");

        if (ticket.Stato == BigliettoState.Validated || ticket.ValidatoAtUtc.HasValue)
            throw new InvalidOperationException("Il biglietto risulta gia validato.");

        ticket.Stato = BigliettoState.Validated;
        ticket.ValidatoAtUtc = DateTime.UtcNow;
        ticket.ValidatoDaUserId = operatorUserId;
        ticket.ValidatoCinemaId = request.CinemaId;

        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        var lookup = await _bigliettoService.GetTicketValidationLookupAsync(ticket.CodiceBiglietto)
            ?? throw new KeyNotFoundException("Biglietto non trovato.");

        return new TicketValidationResultDTO
        {
            Success = true,
            Message = "Biglietto validato con successo.",
            Ticket = lookup
        };
    }

    private static string NormalizeCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("CodiceBiglietto obbligatorio.");

        return code.Trim().ToUpperInvariant();
    }
}
