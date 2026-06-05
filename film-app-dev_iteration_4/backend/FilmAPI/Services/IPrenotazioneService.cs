using FilmAPI.DTO;

namespace FilmAPI.Services;

/// <summary>
///     Servizio per la gestione delle prenotazioni.
///     Le prenotazioni sono ordini semplificati per la prenotazione
///     di posti senza pagamento immediato (es. prenotazione fisica al cinema).
/// </summary>
public interface IPrenotazioneService
{
    /// <summary>Recupera le prenotazioni di un utente specifico.</summary>
    Task<List<PrenotazioneDTO>> GetPrenotazioniAsync(int userId);

    /// <summary>Recupera TUTTE le prenotazioni (solo admin).</summary>
    Task<List<PrenotazioneDTO>> GetAllPrenotazioniAsync();

    /// <summary>Crea una nuova prenotazione per un utente.</summary>
    Task<PrenotazioneDTO?> CreatePrenotazioneAsync(int userId, PrenotazioneCreateDTO dto);

    /// <summary>Cancella una prenotazione (solo proprietario o admin).</summary>
    Task<bool> DeletePrenotazioneAsync(int userId, int prenotazioneId);
}
