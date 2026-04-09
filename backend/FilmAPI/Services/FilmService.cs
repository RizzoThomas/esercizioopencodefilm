using FilmAPI.Data;
using FilmAPI.DTO;
using FilmAPI.Model;
using Microsoft.EntityFrameworkCore;

namespace FilmAPI.Services;

public interface IFilmService
{
    Task<List<FilmDTO>> GetAllAsync();
    Task<FilmDTO?> GetByIdAsync(int id);
    Task<FilmDTO> CreateAsync(FilmCreateDTO dto);
    Task<FilmDTO?> UpdateAsync(int id, FilmUpdateDTO dto);
    Task<bool> DeleteAsync(int id);
}

public class FilmService : IFilmService
{
    private readonly FilmDbContext _context;
    private readonly string _defaultCoverPath;

    public FilmService(FilmDbContext context)
    {
        _context = context;
        _defaultCoverPath = Environment.GetEnvironmentVariable("DEFAULT_COVER_IMAGE_PATH") ?? "/media/defaults/cover-default.jpg";
    }

    public async Task<List<FilmDTO>> GetAllAsync()
    {
        return await _context.Films
            .Select(f => new FilmDTO
            {
                Id = f.Id,
                Titolo = f.Titolo,
                DataProduzione = f.DataProduzione,
                RegistaId = f.RegistaId,
                Durata = f.Durata,
                CopertinaPath = f.CopertinaPath ?? _defaultCoverPath,
                FilmatoPath = f.FilmatoPath
            })
            .ToListAsync();
    }

    public async Task<FilmDTO?> GetByIdAsync(int id)
    {
        var film = await _context.Films.FindAsync(id);
        if (film is null) return null;

        return new FilmDTO
        {
            Id = film.Id,
            Titolo = film.Titolo,
            DataProduzione = film.DataProduzione,
            RegistaId = film.RegistaId,
            Durata = film.Durata,
            CopertinaPath = film.CopertinaPath ?? _defaultCoverPath,
            FilmatoPath = film.FilmatoPath
        };
    }

    public async Task<FilmDTO> CreateAsync(FilmCreateDTO dto)
    {
        var regista = await _context.Registi.FindAsync(dto.RegistaId);
        if (regista is null)
        {
            throw new ArgumentException("Regista non trovato");
        }

        var film = new Film
        {
            Titolo = dto.Titolo,
            DataProduzione = dto.DataProduzione,
            RegistaId = dto.RegistaId,
            Durata = dto.Durata,
            CopertinaPath = string.IsNullOrEmpty(dto.CopertinaPath) ? _defaultCoverPath : dto.CopertinaPath,
            FilmatoPath = dto.FilmatoPath
        };

        _context.Films.Add(film);
        await _context.SaveChangesAsync();

        return new FilmDTO
        {
            Id = film.Id,
            Titolo = film.Titolo,
            DataProduzione = film.DataProduzione,
            RegistaId = film.RegistaId,
            Durata = film.Durata,
            CopertinaPath = film.CopertinaPath,
            FilmatoPath = film.FilmatoPath
        };
    }

    public async Task<FilmDTO?> UpdateAsync(int id, FilmUpdateDTO dto)
    {
        var film = await _context.Films.FindAsync(id);
        if (film is null) return null;

        var regista = await _context.Registi.FindAsync(dto.RegistaId);
        if (regista is null)
        {
            throw new ArgumentException("Regista non trovato");
        }

        film.Titolo = dto.Titolo;
        film.DataProduzione = dto.DataProduzione;
        film.RegistaId = dto.RegistaId;
        film.Durata = dto.Durata;
        film.CopertinaPath = string.IsNullOrEmpty(dto.CopertinaPath) ? _defaultCoverPath : dto.CopertinaPath;
        film.FilmatoPath = dto.FilmatoPath;

        await _context.SaveChangesAsync();

        return new FilmDTO
        {
            Id = film.Id,
            Titolo = film.Titolo,
            DataProduzione = film.DataProduzione,
            RegistaId = film.RegistaId,
            Durata = film.Durata,
            CopertinaPath = film.CopertinaPath,
            FilmatoPath = film.FilmatoPath
        };
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var film = await _context.Films.FindAsync(id);
        if (film is null) return false;

        _context.Films.Remove(film);
        await _context.SaveChangesAsync();
        return true;
    }
}
