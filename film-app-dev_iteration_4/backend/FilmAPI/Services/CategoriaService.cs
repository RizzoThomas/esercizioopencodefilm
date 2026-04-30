using FilmAPI.Data;
using FilmAPI.DTO;
using FilmAPI.Model;
using Microsoft.EntityFrameworkCore;

namespace FilmAPI.Services;

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
            .AsNoTracking()
            .Select(c => new CategoriaDTO
            {
                Id = c.Id,
                Nome = c.Nome
            })
            .ToListAsync();
    }

    public async Task<CategoriaDTO?> GetByIdAsync(int id)
    {
        var categoria = await _context.Categorie
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id);

        if (categoria is null) return null;

        return new CategoriaDTO
        {
            Id = categoria.Id,
            Nome = categoria.Nome
        };
    }

    public async Task<CategoriaDTO> CreateAsync(CategoriaCreateDTO dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Nome))
        {
            throw new ArgumentException("Il nome della categoria e obbligatorio");
        }

        var normalized = dto.Nome.Trim();
        var exists = await _context.Categorie.AnyAsync(c => c.Nome == normalized);
        if (exists)
        {
            throw new InvalidOperationException($"Categoria '{normalized}' gia esistente");
        }

        var categoria = new Categoria { Nome = normalized };
        _context.Categorie.Add(categoria);
        await _context.SaveChangesAsync();

        return new CategoriaDTO
        {
            Id = categoria.Id,
            Nome = categoria.Nome
        };
    }

    public async Task<CategoriaDTO?> UpdateAsync(int id, CategoriaUpdateDTO dto)
    {
        var categoria = await _context.Categorie.FindAsync(id);
        if (categoria is null) return null;

        if (string.IsNullOrWhiteSpace(dto.Nome))
        {
            throw new ArgumentException("Il nome della categoria e obbligatorio");
        }

        var normalized = dto.Nome.Trim();
        var exists = await _context.Categorie.AnyAsync(c => c.Nome == normalized && c.Id != id);
        if (exists)
        {
            throw new InvalidOperationException($"Categoria '{normalized}' gia esistente");
        }

        categoria.Nome = normalized;
        await _context.SaveChangesAsync();

        return new CategoriaDTO
        {
            Id = categoria.Id,
            Nome = categoria.Nome
        };
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var categoria = await _context.Categorie.FindAsync(id);
        if (categoria is null) return false;

        _context.Categorie.Remove(categoria);
        await _context.SaveChangesAsync();
        return true;
    }
}
