using FilmAPI.DTO;

namespace FilmAPI.Services;

public interface ICinemaService
{
    Task<List<CinemaDTO>> GetAllAsync();
    Task<CinemaPagedResultDTO> GetPagedAsync(int page, int pageSize, string? search);
    Task<CinemaDTO?> GetByIdAsync(int id);
    Task<CinemaDTO> CreateAsync(CinemaCreateDTO dto);
    Task<CinemaDTO?> UpdateAsync(int id, CinemaUpdateDTO dto);
    Task<bool> DeleteAsync(int id);
}
