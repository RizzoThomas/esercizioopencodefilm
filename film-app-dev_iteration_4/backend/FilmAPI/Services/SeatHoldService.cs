// ============================================================================
// SeatHoldService.cs — SERVIZIO DI HOLD POSTI (CUORE DEL CHECKOUT)
// ============================================================================
// Gestisce la selezione temporanea dei posti durante l'acquisto.
// Logica principale:
//   1. GetSeatMapAsync: calcola lo stato di ogni posto (libero/tenuto/venduto)
//   2. CreateHoldAsync: blocca posti in transazione atomica
//   3. RefreshHoldAsync: estende la scadenza dell'hold (keep-alive)
//   4. ReleaseHoldAsync: rilascia esplicitamente i posti
//
// CONCETTI CHIAVE:
//   - HoldToken: identificatore univoco della sessione di hold
//   - TTL (Time To Live): tempo dopo cui l'hold scade automaticamente
//   - Cleanup: gli hold scaduti vengono rimossi periodicamente
//   - Concorrenza: gestita con transazioni atomiche del database
// ============================================================================

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
public class SeatHoldService : ISeatHoldService
{
    private readonly FilmDbContext _db;
    private readonly TimeSpan _holdTtl;
    private const int MaxSeatsPerOrder = 10;  // Massimo 10 posti acquistabili

    /// <summary>
    /// Esegue l''operazione SeatHoldService del servizio.
    /// </summary>
    /// <param name="db">Parametro necessario per l'operazione: db.</param>
    /// <param name="configuration">Parametro necessario per l'operazione: configuration.</param>
    /// <returns>Restituisce il risultato dell'operazione quando questa ha esito positivo; altrimenti il chiamante riceve un'eccezione o un risultato nullo/booleano secondo il contratto del metodo.</returns>
    /// <remarks>
    /// Effetti collaterali: scrive o aggiorna il database.
    /// </remarks>
    public SeatHoldService(FilmDbContext db, IConfiguration? configuration = null)
    {
        _db = db;
        // Legge TTL da configurazione (default 10 minuti)
        var holdTtlMinutes = configuration != null
            ? int.Parse(configuration["HOLD_TTL_MINUTES"] ?? "10")
            : 10;
        _holdTtl = TimeSpan.FromMinutes(holdTtlMinutes);
    }

    // ========================================================================
    // GET SEAT MAP — Calcola la piantina dei posti con stato aggiornato
    // ========================================================================
    // Per ogni posto della sala, determina se è:
    //   Available (0)     — Posto libero
    //   HeldByOther (1)   — Tenuto da un altro utente
    //   HeldByMe (2)      — Tenuto dall'utente corrente
    //   Sold (3)          — Già venduto
    // ========================================================================
    /// <summary>
    /// Recupera o legge i dati tramite l''operazione GetSeatMapAsync del servizio.
    /// </summary>
    /// <param name="showId">Identificativo necessario per individuare l'entità o il contesto di lavoro: showId.</param>
    /// <param name="userId">Identificativo necessario per individuare l'entità o il contesto di lavoro: userId.</param>
    /// <returns>Restituisce in modo asincrono il risultato dell'operazione indicato dal tipo interno del Task quando la logica termina correttamente.</returns>
    /// <remarks>
    /// Effetti collaterali: scrive o aggiorna il database.
    /// </remarks>
    public async Task<SeatMapDTO> GetSeatMapAsync(int showId, int userId)
    {
        var now = DateTime.UtcNow;

        // Prima pulisce gli hold scaduti (lazy cleanup)
        await CleanupExpiredHoldsForShowAsync(showId);

        // Carica show con tutte le entità correlate
        var show = await _db.Shows
            .Include(s => s.Sala)
            .Include(s => s.Film)
            .Include(s => s.Cinema)
            .FirstOrDefaultAsync(s => s.Id == showId);

        if (show == null)
            throw new InvalidOperationException("Show non trovato.");

        // Carica tutti i posti della sala
        var posti = await _db.SalaPosti
            .Where(p => p.SalaId == show.SalaId && p.IsAttivo)
            .ToListAsync();

        // Carica tutti gli stati (hold/sold) per questo show
        var stati = await _db.ShowPostiStato
            .Where(sps => sps.ShowId == showId)
            .ToListAsync();

        // Crea un dizionario per lookup veloce: SalaPostoId → stato
        var statiDict = stati.ToDictionary(sps => sps.SalaPostoId);

        var seatInfos = new List<SeatInfoDTO>();
        DateTime? myScadeAtUtc = null;

        // Per OGNI posto, determina lo stato in base ai dati del DB
        foreach (var posto in posti)
        {
            var status = SeatStatus.Available;  // Default: libero

            if (statiDict.TryGetValue(posto.Id, out var stato))
            {
                if (stato.Stato == ShowPostoState.Sold)
                {
                    status = SeatStatus.Sold;               // Venduto
                }
                else if (stato.Stato == ShowPostoState.Hold)
                {
                    if (stato.ScadeAtUtc <= now)
                    {
                        status = SeatStatus.Available;      // Hold scaduto = libero
                    }
                    else if (stato.UserId == userId)
                    {
                        status = SeatStatus.HeldByMe;       // Mio hold
                        if (stato.ScadeAtUtc.HasValue)
                            myScadeAtUtc = stato.ScadeAtUtc.Value;
                    }
                    else
                    {
                        status = SeatStatus.HeldByOther;    // Hold di altro utente
                    }
                }
            }
            // Se non c'è stato nel DB → Available

            seatInfos.Add(new SeatInfoDTO
            {
                SalaPostoId = posto.Id,
                Settore = posto.Settore,
                Fila = posto.Fila,
                Numero = posto.Numero,
                IsWheelchair = posto.IsWheelchair,
                Stato = status
            });
        }

        return new SeatMapDTO
        {
            ShowId = showId,
            FilmTitolo = show.Film!.Titolo,
            CinemaNome = show.Cinema!.Nome,
            SalaNome = show.Sala.Nome ?? $"Sala {show.Sala.NumeroProgressivo}",
            StartAtUtc = show.StartAtUtc,
            PrezzoBase = TicketPriceNormalizer.NormalizeUnitPrice(show.PrezzoBase),
            SupplementoSala = TicketPriceNormalizer.NormalizeUnitPrice(show.SupplementoSala),
            ScadeAtUtc = myScadeAtUtc,
            Posti = seatInfos
        };
    }

    // ========================================================================
    // CREATE HOLD — Blocca posti in modo atomico
    // ========================================================================
    // Operazioni in TRANSAZIONE:
    //   1. Cleanup hold scaduti
    //   2. Verifica che i posti appartengano alla sala
    //   3. Controlla conflitti con altri utenti
    //   4. Crea record ShowPostoStato per ogni posto
    //
    // Se c'è un conflitto → rollback + restituisci 409
    // Se tutto OK → commit + restituisci holdToken
    // ========================================================================
    /// <summary>
    /// Esegue l''operazione di business CreateHoldAsync del servizio.
    /// </summary>
    /// <param name="showId">Identificativo necessario per individuare l'entità o il contesto di lavoro: showId.</param>
    /// <param name="userId">Identificativo necessario per individuare l'entità o il contesto di lavoro: userId.</param>
    /// <param name="salaPostoIds">Parametro necessario per l'operazione: salaPostoIds.</param>
    /// <returns>Restituisce in modo asincrono il risultato dell'operazione indicato dal tipo interno del Task quando la logica termina correttamente.</returns>
    /// <remarks>
    /// Effetti collaterali: scrive o aggiorna il database.
    /// </remarks>
    public async Task<SeatHoldResponseDTO> CreateHoldAsync(int showId, int userId, List<int> salaPostoIds)
    {
        // Validazioni di base
        if (salaPostoIds.Count == 0)
            throw new ArgumentException("Nessun posto selezionato.");
        if (salaPostoIds.Count > MaxSeatsPerOrder)
            throw new ArgumentException($"Massimo {MaxSeatsPerOrder} posti per ordine.");

        var now = DateTime.UtcNow;
        var expiresAt = now.Add(_holdTtl);

        // === INIZIO TRANSAZIONE ===
        // using var transaction = begin/commit/rollback automatico
        await using var transaction = await _db.Database.BeginTransactionAsync();

        // PASSO 1: pulisci hold scaduti
        await CleanupExpiredHoldsForShowAsync(showId);

        // PASSO 2: verifica che lo show esista
        var show = await _db.Shows
            .Include(s => s.Sala)
            .FirstOrDefaultAsync(s => s.Id == showId);
        if (show == null)
            throw new InvalidOperationException("Show non trovato.");

        // PASSO 3: verifica che tutti i posti appartengano alla sala
        var postiValidi = await _db.SalaPosti
            .Where(p => salaPostoIds.Contains(p.Id) && p.SalaId == show.SalaId && p.IsAttivo)
            .ToListAsync();
        if (postiValidi.Count != salaPostoIds.Count)
            throw new ArgumentException("Uno o piu posti non appartengono alla sala dello show o non sono attivi.");

        // PASSO 4: controlla conflitti con altri utenti
        var statiEsistenti = await _db.ShowPostiStato
            .Where(sps => sps.ShowId == showId && salaPostoIds.Contains(sps.SalaPostoId))
            .ToListAsync();

        var conflitti = new List<string>();
        var postiDaAcquisire = new List<int>();

        foreach (var postoId in salaPostoIds)
        {
            var esistente = statiEsistenti.FirstOrDefault(sps => sps.SalaPostoId == postoId);
            if (esistente != null)
            {
                if (esistente.Stato == ShowPostoState.Sold)
                {
                    conflitti.Add($"Posto {postoId} gia venduto.");
                }
                else if (esistente.Stato == ShowPostoState.Hold)
                {
                    if (esistente.ScadeAtUtc > now)
                    {
                        if (esistente.UserId == userId)
                            postiDaAcquisire.Add(postoId);  // Mio hold → aggiorno
                        else
                            conflitti.Add($"Posto {postoId} gia prenotato da altro utente.");
                    }
                    else
                    {
                        postiDaAcquisire.Add(postoId);  // Hold scaduto → posso prenderlo
                    }
                }
            }
            else
            {
                postiDaAcquisire.Add(postoId);  // Nessun conflitto → libero
            }
        }

        // Se ci sono conflitti → rollback e restituisci conflitti
        if (conflitti.Count > 0)
        {
            await transaction.RollbackAsync();
            return new SeatHoldResponseDTO
            {
                HoldToken = string.Empty,
                ScadeAtUtc = expiresAt,
                SalaPostoIds = new List<int>(),
                Conflitti = conflitti
            };
        }

        // PASSO 5: crea/aggiorna gli hold
        var holdToken = $"{userId}_{showId}_{Guid.NewGuid():N}";  // Token univoco

        foreach (var postoId in postiDaAcquisire)
        {
            var stato = statiEsistenti.FirstOrDefault(sps => sps.SalaPostoId == postoId);
            if (stato == null)
            {
                // Nuovo record
                stato = new ShowPostoStato
                {
                    ShowId = showId,
                    SalaPostoId = postoId,
                    UserId = userId,
                    Stato = ShowPostoState.Hold,
                    HoldToken = holdToken,
                    ScadeAtUtc = expiresAt,
                    UpdatedAtUtc = now
                };
                _db.ShowPostiStato.Add(stato);
            }
            else
            {
                // Aggiorna record esistente (mio hold o hold scaduto)
                stato.UserId = userId;
                stato.Stato = ShowPostoState.Hold;
                stato.HoldToken = holdToken;
                stato.ScadeAtUtc = expiresAt;
                stato.UpdatedAtUtc = now;
            }
        }

        await _db.SaveChangesAsync();
        await transaction.CommitAsync();
        // === FINE TRANSAZIONE ===

        return new SeatHoldResponseDTO
        {
            HoldToken = holdToken,
            ScadeAtUtc = expiresAt,
            SalaPostoIds = postiDaAcquisire,
            Conflitti = conflitti
        };
    }

    // ========================================================================
    // REFRESH HOLD — Estende il TTL di un hold esistente (keep-alive)
    // Il frontend chiama questo endpoint ogni 60 secondi
    // ========================================================================
    /// <summary>
    /// Esegue l''operazione di business RefreshHoldAsync del servizio.
    /// </summary>
    /// <param name="holdToken">Token necessario per validare, rinnovare o revocare l'operazione richiesta.</param>
    /// <param name="userId">Identificativo necessario per individuare l'entità o il contesto di lavoro: userId.</param>
    /// <returns>Restituisce in modo asincrono il risultato dell'operazione indicato dal tipo interno del Task quando la logica termina correttamente.</returns>
    /// <remarks>
    /// Effetti collaterali: scrive o aggiorna il database.
    /// </remarks>
    public async Task<SeatHoldResponseDTO> RefreshHoldAsync(string holdToken, int userId)
    {
        var now = DateTime.UtcNow;
        var newExpiresAt = now.Add(_holdTtl);

        // Cerca tutti i posti con questo hold token
        var stati = await _db.ShowPostiStato
            .Where(sps => sps.HoldToken == holdToken && sps.UserId == userId)
            .ToListAsync();

        if (stati.Count == 0)
            throw new InvalidOperationException("Hold non trovato.");

        // Estende la scadenza di tutti i posti tenuti
        foreach (var stato in stati)
        {
            stato.ScadeAtUtc = newExpiresAt;
            stato.UpdatedAtUtc = now;
        }

        await _db.SaveChangesAsync();

        return new SeatHoldResponseDTO
        {
            HoldToken = holdToken,
            ScadeAtUtc = newExpiresAt,
            SalaPostoIds = stati.Select(s => s.SalaPostoId).ToList(),
            Conflitti = new List<string>()
        };
    }

    // ========================================================================
    // RELEASE HOLD — Rilascia esplicitamente un hold
    // Chiamato quando l'utente deseleziona tutti i posti
    // ========================================================================
    /// <summary>
    /// Esegue l''operazione ReleaseHoldAsync del servizio.
    /// </summary>
    /// <param name="holdToken">Token necessario per validare, rinnovare o revocare l'operazione richiesta.</param>
    /// <param name="userId">Identificativo necessario per individuare l'entità o il contesto di lavoro: userId.</param>
    /// <returns>Restituisce in modo asincrono il risultato dell'operazione indicato dal tipo interno del Task quando la logica termina correttamente.</returns>
    /// <remarks>
    /// Effetti collaterali: scrive o aggiorna il database.
    /// </remarks>
    public async Task<bool> ReleaseHoldAsync(string holdToken, int userId)
    {
        var stati = await _db.ShowPostiStato
            .Where(sps => sps.HoldToken == holdToken && sps.UserId == userId)
            .ToListAsync();

        if (stati.Count == 0) return false;

        _db.ShowPostiStato.RemoveRange(stati);
        await _db.SaveChangesAsync();
        return true;
    }

    // ========================================================================
    // CLEANUP (privato) — Rimuove hold scaduti per uno show specifico
    // Chiamato automaticamente prima di GetSeatMap e CreateHold
    // ========================================================================
    private async Task CleanupExpiredHoldsForShowAsync(int showId)
    {
        var now = DateTime.UtcNow;
        var expired = await _db.ShowPostiStato
            .Where(sps => sps.ShowId == showId
                   && sps.Stato == ShowPostoState.Hold
                   && sps.ScadeAtUtc <= now)
            .ToListAsync();

        if (expired.Count > 0)
        {
            _db.ShowPostiStato.RemoveRange(expired);
            await _db.SaveChangesAsync();
        }
    }

    // ========================================================================
    // CLEANUP (pubblico) — Rimuove TUTTI gli hold scaduti a livello globale
    // Chiamato dal hosted service ExpiredHoldCleanupService ogni 5 minuti
    // Restituisce il numero di record puliti
    // ========================================================================
    /// <summary>
    /// Esegue l''operazione CleanupExpiredHoldsAsync del servizio.
    /// </summary>
    /// <returns>Restituisce in modo asincrono il risultato dell'operazione indicato dal tipo interno del Task quando la logica termina correttamente.</returns>
    /// <remarks>
    /// Effetti collaterali: scrive o aggiorna il database.
    /// </remarks>
    public async Task<int> CleanupExpiredHoldsAsync()
    {
        var now = DateTime.UtcNow;
        var expired = await _db.ShowPostiStato
            .Where(sps => sps.Stato == ShowPostoState.Hold
                   && sps.ScadeAtUtc <= now)
            .ToListAsync();

        if (expired.Count > 0)
        {
            _db.ShowPostiStato.RemoveRange(expired);
            await _db.SaveChangesAsync();
        }
        return expired.Count;
    }
}
