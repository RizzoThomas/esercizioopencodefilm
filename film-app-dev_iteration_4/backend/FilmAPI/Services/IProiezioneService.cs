using FilmAPI.DTO;

namespace FilmAPI.Services;

/// <summary>
///     Servizio per la gestione delle proiezioni (show).
///     CRUD completo con paginazione e ricerca.
///     Ogni proiezione lega un film a un cinema, una sala e un orario.
/// </summary>
public interface IProiezioneService
{
    /// <summary>Recupera tutte le proiezioni disponibili.</summary>
    Task<List<ProiezioneDTO>> GetAllAsync();

    /// <summary>Recupera proiezioni con paginazione e ricerca.</summary>
    Task<ProiezionePagedResultDTO> GetPagedAsync(int page, int pageSize, string? search);

    /// <summary>Recupera una proiezione per ID.</summary>
    Task<ProiezioneDTO?> GetByIdAsync(int id);

    /// <summary>Crea una nuova proiezione (admin/cinema staff).</summary>
    Task<ProiezioneDTO> CreateAsync(ProiezioneCreateDTO dto);

    /// <summary>Aggiorna una proiezione esistente.</summary>
    Task<ProiezioneDTO?> UpdateAsync(int id, ProiezioneUpdateDTO dto);

    /// <summary>Elimina una proiezione.</summary>
    Task<bool> DeleteAsync(int id);
}
