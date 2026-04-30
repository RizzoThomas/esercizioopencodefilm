using FilmAPI.DTO;

namespace FilmAPI.Services;

public interface IPrenotazioneService
{
    Task<List<PrenotazioneDTO>> GetPrenotazioniAsync(int userId);
    Task<List<PrenotazioneDTO>> GetAllPrenotazioniAsync();
    Task<PrenotazioneDTO?> CreatePrenotazioneAsync(int userId, PrenotazioneCreateDTO dto);
    Task<bool> DeletePrenotazioneAsync(int userId, int prenotazioneId);
}
