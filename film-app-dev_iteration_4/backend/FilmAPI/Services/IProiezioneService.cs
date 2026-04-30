using FilmAPI.DTO;

namespace FilmAPI.Services;

public interface IProiezioneService
{
    Task<List<ProiezioneDTO>> GetAllAsync();
    Task<ProiezionePagedResultDTO> GetPagedAsync(int page, int pageSize, string? search);
    Task<ProiezioneDTO?> GetByIdAsync(int id);
    Task<ProiezioneDTO> CreateAsync(ProiezioneCreateDTO dto);
    Task<ProiezioneDTO?> UpdateAsync(int id, ProiezioneUpdateDTO dto);
    Task<bool> DeleteAsync(int id);
}
