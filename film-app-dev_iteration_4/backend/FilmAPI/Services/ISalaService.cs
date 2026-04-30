using FilmAPI.DTO;

namespace FilmAPI.Services;

public interface ISalaService
{
    Task<List<SalaDTO>> GetByCinemaAsync(int cinemaId);
    Task<SalaDTO?> GetByIdAsync(int id);
    Task<SalaDTO> CreateAsync(SalaCreateDTO dto);
    Task<SalaDTO?> UpdateAsync(int id, SalaUpdateDTO dto);
    Task<bool> DeleteAsync(int id);
    Task<List<SalaPostoDTO>> GetPostiAsync(int salaId);
    Task<List<SalaPostoDTO>> SavePostiAsync(int salaId, SalaLayoutSaveDTO dto);
}
