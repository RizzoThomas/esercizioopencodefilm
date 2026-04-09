using FilmAPI.Data;
using FilmAPI.DTO.Categoria;
using FilmAPI.Model;
using Microsoft.EntityFrameworkCore;

namespace FilmAPI.Services;

public interface ICategoriaService
{
    Task<List<CategoriaDTO>> GetAllAsync();
    Task<CategoriaDTO?> GetByIdAsync(int id);
    Task<CategoriaDTO> CreateAsync(CategoriaCreateDTO dto);
    Task<CategoriaDTO?> UpdateAsync(int id, CategoriaCreateDTO dto);
    Task<bool> DeleteAsync(int id);
    Task<List<CategoriaDTO>> GetCategorieByFilmIdAsync(int filmId);
    Task AddCategoriaToFilmAsync(int filmId, int categoriaId);
    Task RemoveCategoriaFromFilmAsync(int filmId, int categoriaId);
}

public class CategoriaService : ICategoriaService
{
    private readonly FilmDbContext _context;

    public CategoriaService(FilmDbContext context)
    {
        _context = context;
    }

    public async Task<List<CategoriaDTO>> GetAllAsync()
    {
        return await _context.Categorie
            .Select(c => new CategoriaDTO(
                c.Id,
                c.Nome,
                c.Descrizione
            ))
            .ToListAsync();
    }

    public async Task<CategoriaDTO?> GetByIdAsync(int id)
    {
        var categoria = await _context.Categorie
            .FirstOrDefaultAsync(c => c.Id == id);

        if (categoria == null) return null;

        return new CategoriaDTO(
            categoria.Id,
            categoria.Nome,
            categoria.Descrizione
        );
    }

    public async Task<CategoriaDTO> CreateAsync(CategoriaCreateDTO dto)
    {
        var categoria = new Categoria
        {
            Nome = dto.Nome,
            Descrizione = dto.Descrizione
        };

        _context.Categorie.Add(categoria);
        await _context.SaveChangesAsync();

        return new CategoriaDTO(
            categoria.Id,
            categoria.Nome,
            categoria.Descrizione
        );
    }

    public async Task<CategoriaDTO?> UpdateAsync(int id, CategoriaCreateDTO dto)
    {
        var categoria = await _context.Categorie
            .FirstOrDefaultAsync(c => c.Id == id);

        if (categoria == null) return null;

        categoria.Nome = dto.Nome;
        categoria.Descrizione = dto.Descrizione;

        await _context.SaveChangesAsync();

        return new CategoriaDTO(
            categoria.Id,
            categoria.Nome,
            categoria.Descrizione
        );
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var categoria = await _context.Categorie
            .FirstOrDefaultAsync(c => c.Id == id);

        if (categoria == null) return false;

        _context.Categorie.Remove(categoria);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<List<CategoriaDTO>> GetCategorieByFilmIdAsync(int filmId)
    {
        return await _context.FilmCategorie
            .Where(fc => fc.FilmId == filmId)
            .Select(fc => new CategoriaDTO(
                fc.Categoria.Id,
                fc.Categoria.Nome,
                fc.Categoria.Descrizione
            ))
            .ToListAsync();
    }

    public async Task AddCategoriaToFilmAsync(int filmId, int categoriaId)
    {
        var film = await _context.Films.FindAsync(filmId);
        if (film == null)
            throw new ArgumentException("Film non trovato");

        var categoria = await _context.Categorie.FindAsync(categoriaId);
        if (categoria == null)
            throw new ArgumentException("Categoria non trovata");

        var existing = await _context.FilmCategorie
            .AnyAsync(fc => fc.FilmId == filmId && fc.CategoriaId == categoriaId);

        if (!existing)
        {
            _context.FilmCategorie.Add(new FilmCategoria
            {
                FilmId = filmId,
                CategoriaId = categoriaId
            });
            await _context.SaveChangesAsync();
        }
    }

    public async Task RemoveCategoriaFromFilmAsync(int filmId, int categoriaId)
    {
        var filmCategoria = await _context.FilmCategorie
            .FirstOrDefaultAsync(fc => fc.FilmId == filmId && fc.CategoriaId == categoriaId);

        if (filmCategoria != null)
        {
            _context.FilmCategorie.Remove(filmCategoria);
            await _context.SaveChangesAsync();
        }
    }
}
