using FilmAPI.DTO;

namespace FilmAPI.Services;

public interface IShowService
{
    Task<ShowPagedResultDTO> GetPagedAsync(int page, int pageSize, int? cinemaId = null, int? filmId = null, DateTime? date = null);
    Task<List<ShowDTO>> GetAllAsync();
    Task<ShowDTO?> GetByIdAsync(int id);
    Task<ShowDTO> CreateAsync(ShowCreateDTO dto);
    Task<ShowDTO?> UpdateAsync(int id, ShowUpdateDTO dto);
    Task<bool> DeleteAsync(int id);
    Task<List<ShowDTO>> GetByCinemaAsync(int cinemaId);
    Task<List<ShowDTO>> GetByFilmAsync(int filmId);
    Task<List<ShowDTO>> GetByDateAsync(DateTime date);
}
