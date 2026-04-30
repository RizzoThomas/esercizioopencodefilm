using FilmAPI.DTO;

namespace FilmAPI.Services;

public interface ICategoriaService
{
    Task<List<CategoriaDTO>> GetAllAsync();
    Task<CategoriaDTO?> GetByIdAsync(int id);
    Task<CategoriaDTO> CreateAsync(CategoriaCreateDTO dto);
    Task<CategoriaDTO?> UpdateAsync(int id, CategoriaUpdateDTO dto);
    Task<bool> DeleteAsync(int id);
}
