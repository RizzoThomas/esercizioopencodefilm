using FilmAPI.DTO;

namespace FilmAPI.Services;

public interface IRegistaService
{
    Task<List<RegistaDTO>> GetAllAsync();
    Task<RegistaPagedResultDTO> GetPagedAsync(int page, int pageSize, string? search);
    Task<RegistaDTO?> GetByIdAsync(int id);
    Task<RegistaDTO> CreateAsync(RegistaCreateDTO dto);
    Task<RegistaDTO?> UpdateAsync(int id, RegistaUpdateDTO dto);
    Task<bool> DeleteAsync(int id);
    Task<List<FilmDTO>> GetFilmsByRegistaIdAsync(int id);
}
