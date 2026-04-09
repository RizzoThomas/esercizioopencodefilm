using FilmAPI.Data;
using FilmAPI.DTO;
using FilmAPI.Model;
using Microsoft.EntityFrameworkCore;

namespace FilmAPI.Services;

public interface ICinemaService
{
    Task<List<CinemaDTO>> GetAllAsync();
    Task<CinemaDTO?> GetByIdAsync(int id);
    Task<CinemaDTO> CreateAsync(CinemaCreateDTO dto);
    Task<CinemaDTO?> UpdateAsync(int id, CinemaUpdateDTO dto);
    Task<bool> DeleteAsync(int id);
}

public class CinemaService : ICinemaService
{
    private readonly FilmDbContext _context;

    public CinemaService(FilmDbContext context)
    {
        _context = context;
    }

    public async Task<List<CinemaDTO>> GetAllAsync()
    {
        return await _context.Cinemas
            .Select(c => new CinemaDTO
            {
                Id = c.Id,
                Nome = c.Nome,
                Indirizzo = c.Indirizzo,
                Citta = c.Citta,
                CapienzaTotale = c.CapienzaTotale
            })
            .ToListAsync();
    }

    public async Task<CinemaDTO?> GetByIdAsync(int id)
    {
        var cinema = await _context.Cinemas.FindAsync(id);
        if (cinema is null) return null;

        return new CinemaDTO
        {
            Id = cinema.Id,
            Nome = cinema.Nome,
            Indirizzo = cinema.Indirizzo,
            Citta = cinema.Citta,
            CapienzaTotale = cinema.CapienzaTotale
        };
    }

    public async Task<CinemaDTO> CreateAsync(CinemaCreateDTO dto)
    {
        var cinema = new Cinema
        {
            Nome = dto.Nome,
            Indirizzo = dto.Indirizzo,
            Citta = dto.Citta,
            CapienzaTotale = dto.CapienzaTotale
        };

        _context.Cinemas.Add(cinema);
        await _context.SaveChangesAsync();

        return new CinemaDTO
        {
            Id = cinema.Id,
            Nome = cinema.Nome,
            Indirizzo = cinema.Indirizzo,
            Citta = cinema.Citta,
            CapienzaTotale = cinema.CapienzaTotale
        };
    }

    public async Task<CinemaDTO?> UpdateAsync(int id, CinemaUpdateDTO dto)
    {
        var cinema = await _context.Cinemas.FindAsync(id);
        if (cinema is null) return null;

        cinema.Nome = dto.Nome;
        cinema.Indirizzo = dto.Indirizzo;
        cinema.Citta = dto.Citta;
        cinema.CapienzaTotale = dto.CapienzaTotale;

        await _context.SaveChangesAsync();

        return new CinemaDTO
        {
            Id = cinema.Id,
            Nome = cinema.Nome,
            Indirizzo = cinema.Indirizzo,
            Citta = cinema.Citta,
            CapienzaTotale = cinema.CapienzaTotale
        };
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var cinema = await _context.Cinemas.FindAsync(id);
        if (cinema is null) return false;

        _context.Cinemas.Remove(cinema);
        await _context.SaveChangesAsync();
        return true;
    }
}
