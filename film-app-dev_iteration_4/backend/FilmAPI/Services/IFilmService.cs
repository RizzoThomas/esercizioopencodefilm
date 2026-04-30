using FilmAPI.DTO;

namespace FilmAPI.Services;

public interface IFilmService
{
    Task<List<FilmDTO>> GetAllAsync();
    Task<FilmPagedResultDTO> GetPagedAsync(int page, int pageSize, string? search);
    Task<FilmDTO?> GetByIdAsync(int id);
    Task<FilmDTO> CreateAsync(FilmCreateDTO dto);
    Task<FilmDTO?> UpdateAsync(int id, FilmUpdateDTO dto);
    Task<bool> DeleteAsync(int id);
}
