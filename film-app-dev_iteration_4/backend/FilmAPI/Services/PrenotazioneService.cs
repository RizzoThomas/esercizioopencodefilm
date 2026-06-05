using FilmAPI.Data;
using FilmAPI.DTO;
using FilmAPI.Model;
using Microsoft.EntityFrameworkCore;

namespace FilmAPI.Services;

/// <summary>
///     Servizio per la gestione delle prenotazioni.
///     Recupera le prenotazioni effettuate dall'utente, con dettagli
///     su proiezione, film, cinema, posti e stato del pagamento.
///     Ogni prenotazione è legata a un utente e a una proiezione.
/// </summary>
public class PrenotazioneService : IPrenotazioneService
{
    /// <summary>DbContext per accesso a prenotazioni, proiezioni e utenti.</summary>
    private readonly FilmDbContext _context;

    /// <summary>
    ///     Inizializza il servizio con il contesto del database.
    /// </summary>
    public PrenotazioneService(FilmDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Recupera o legge i dati tramite l''operazione GetPrenotazioniAsync del servizio.
    /// </summary>
    /// <param name="userId">Identificativo necessario per individuare l'entità o il contesto di lavoro: userId.</param>
    /// <returns>Restituisce in modo asincrono il risultato dell'operazione indicato dal tipo interno del Task quando la logica termina correttamente.</returns>
    /// <remarks>
    /// Effetti collaterali: scrive o aggiorna il database.
    /// </remarks>
    public async Task<List<PrenotazioneDTO>> GetPrenotazioniAsync(int userId)
    {
        return await _context.Prenotazioni
            .Where(p => p.UserId == userId)
            .Include(p => p.Proiezione)
                .ThenInclude(pr => pr!.Film)
            .Include(p => p.Proiezione)
                .ThenInclude(pr => pr!.Cinema)
            .Select(p => MapToDTO(p))
            .ToListAsync();
    }

    /// <summary>
    /// Recupera o legge i dati tramite l''operazione GetAllPrenotazioniAsync del servizio.
    /// </summary>
    /// <returns>Restituisce in modo asincrono il risultato dell'operazione indicato dal tipo interno del Task quando la logica termina correttamente.</returns>
    /// <remarks>
    /// Effetti collaterali: scrive o aggiorna il database.
    /// </remarks>
    public async Task<List<PrenotazioneDTO>> GetAllPrenotazioniAsync()
    {
        return await _context.Prenotazioni
            .Include(p => p.Proiezione)
                .ThenInclude(pr => pr!.Film)
            .Include(p => p.Proiezione)
                .ThenInclude(pr => pr!.Cinema)
            .Select(p => MapToDTO(p))
            .ToListAsync();
    }

    /// <summary>
    /// Esegue l''operazione di business CreatePrenotazioneAsync del servizio.
    /// </summary>
    /// <param name="userId">Identificativo necessario per individuare l'entità o il contesto di lavoro: userId.</param>
    /// <param name="dto">Oggetto DTO di input necessario per eseguire l'operazione.</param>
    /// <returns>Restituisce in modo asincrono il risultato dell'operazione indicato dal tipo interno del Task quando la logica termina correttamente.</returns>
    /// <remarks>
    /// Effetti collaterali: scrive o aggiorna il database.
    /// </remarks>
    public async Task<PrenotazioneDTO?> CreatePrenotazioneAsync(int userId, PrenotazioneCreateDTO dto)
    {
        var proiezione = await _context.Proiezioni
            .Include(p => p.Film)
            .Include(p => p.Cinema)
            .FirstOrDefaultAsync(p => p.Id == dto.ProiezioneId);

        if (proiezione is null)
        {
            throw new InvalidOperationException("Proiezione non trovata");
        }

        var prenotazione = new Prenotazione
        {
            UserId = userId,
            ProiezioneId = dto.ProiezioneId,
            NumeroPosti = dto.NumeroPosti,
            Note = dto.Note,
            DataPrenotazione = DateTime.UtcNow
        };

        _context.Prenotazioni.Add(prenotazione);
        await _context.SaveChangesAsync();

        return new PrenotazioneDTO
        {
            Id = prenotazione.Id,
            ProiezioneId = proiezione.Id,
            TitoloFilm = proiezione.Film?.Titolo ?? string.Empty,
            NomeCinema = proiezione.Cinema?.Nome ?? string.Empty,
            DataProiezione = proiezione.Data,
            OraProiezione = proiezione.Ora,
            NumeroPosti = prenotazione.NumeroPosti,
            Note = prenotazione.Note,
            DataPrenotazione = prenotazione.DataPrenotazione
        };
    }

    /// <summary>
    /// Esegue l''operazione di business DeletePrenotazioneAsync del servizio.
    /// </summary>
    /// <param name="userId">Identificativo necessario per individuare l'entità o il contesto di lavoro: userId.</param>
    /// <param name="prenotazioneId">Identificativo necessario per individuare l'entità o il contesto di lavoro: prenotazioneId.</param>
    /// <returns>Restituisce in modo asincrono il risultato dell'operazione indicato dal tipo interno del Task quando la logica termina correttamente.</returns>
    /// <remarks>
    /// Effetti collaterali: scrive o aggiorna il database.
    /// </remarks>
    public async Task<bool> DeletePrenotazioneAsync(int userId, int prenotazioneId)
    {
        var prenotazione = await _context.Prenotazioni
            .FirstOrDefaultAsync(p => p.Id == prenotazioneId && p.UserId == userId);

        if (prenotazione is null) return false;

        _context.Prenotazioni.Remove(prenotazione);
        await _context.SaveChangesAsync();
        return true;
    }

    private static PrenotazioneDTO MapToDTO(Prenotazione prenotazione)
    {
        return new PrenotazioneDTO
        {
            Id = prenotazione.Id,
            ProiezioneId = prenotazione.ProiezioneId,
            TitoloFilm = prenotazione.Proiezione?.Film?.Titolo ?? string.Empty,
            NomeCinema = prenotazione.Proiezione?.Cinema?.Nome ?? string.Empty,
            DataProiezione = prenotazione.Proiezione?.Data ?? DateTime.MinValue,
            OraProiezione = prenotazione.Proiezione?.Ora ?? DateTime.MinValue,
            NumeroPosti = prenotazione.NumeroPosti,
            Note = prenotazione.Note,
            DataPrenotazione = prenotazione.DataPrenotazione
        };
    }
}
