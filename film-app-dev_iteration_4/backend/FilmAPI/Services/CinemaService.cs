using FilmAPI.Data;
using FilmAPI.DTO;
using FilmAPI.Model;
using Microsoft.EntityFrameworkCore;

namespace FilmAPI.Services;

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
                Citta = c.Citta
            })
            .ToListAsync();
    }

    public async Task<CinemaPagedResultDTO> GetPagedAsync(int page, int pageSize, string? search)
    {
        var normalizedPage = page < 1 ? 1 : page;
        var normalizedPageSize = pageSize < 1 ? 10 : pageSize;

        var query = _context.Cinemas.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var likePattern = $"%{search.Trim()}%";
            query = query.Where(c =>
                EF.Functions.Like(c.Nome, likePattern) ||
                EF.Functions.Like(c.Citta, likePattern) ||
                EF.Functions.Like(c.Indirizzo, likePattern));
        }

        var totalCount = await query.CountAsync();
        var totalPages = totalCount == 0
            ? 1
            : (int)Math.Ceiling(totalCount / (double)normalizedPageSize);

        if (normalizedPage > totalPages)
        {
            normalizedPage = totalPages;
        }

        var items = await query
            .OrderBy(c => c.Id)
            .Skip((normalizedPage - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .Select(c => new CinemaDTO
            {
                Id = c.Id,
                Nome = c.Nome,
                Indirizzo = c.Indirizzo,
                Citta = c.Citta
            })
            .ToListAsync();

        return new CinemaPagedResultDTO
        {
            Items = items,
            Page = normalizedPage,
            PageSize = normalizedPageSize,
            TotalCount = totalCount,
            TotalPages = totalPages
        };
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
            Citta = cinema.Citta
        };
    }

    public async Task<CinemaDTO> CreateAsync(CinemaCreateDTO dto)
    {
        var cinema = new Cinema
        {
            Nome = dto.Nome,
            Indirizzo = dto.Indirizzo,
            Citta = dto.Citta
        };

        _context.Cinemas.Add(cinema);
        await _context.SaveChangesAsync();

        return new CinemaDTO
        {
            Id = cinema.Id,
            Nome = cinema.Nome,
            Indirizzo = cinema.Indirizzo,
            Citta = cinema.Citta
        };
    }

    public async Task<CinemaDTO?> UpdateAsync(int id, CinemaUpdateDTO dto)
    {
        var cinema = await _context.Cinemas.FindAsync(id);
        if (cinema is null) return null;

        cinema.Nome = dto.Nome;
        cinema.Indirizzo = dto.Indirizzo;
        cinema.Citta = dto.Citta;

        await _context.SaveChangesAsync();

        return new CinemaDTO
        {
            Id = cinema.Id,
            Nome = cinema.Nome,
            Indirizzo = cinema.Indirizzo,
            Citta = cinema.Citta
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
