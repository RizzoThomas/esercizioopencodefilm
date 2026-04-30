using FilmAPI.Data;
using FilmAPI.DTO;
using FilmAPI.Model;
using Microsoft.EntityFrameworkCore;

namespace FilmAPI.Services;

public class RegistaService : IRegistaService
{
    private readonly FilmDbContext _context;
    private readonly string _defaultCoverPath;

    public RegistaService(FilmDbContext context)
    {
        _context = context;
        _defaultCoverPath = Environment.GetEnvironmentVariable("DEFAULT_COVER_IMAGE_PATH") ?? "/media/defaults/cover-default.jpg";
    }

    public async Task<List<RegistaDTO>> GetAllAsync()
    {
        return await _context.Registi
            .Select(r => new RegistaDTO
            {
                Id = r.Id,
                Nome = r.Nome,
                Cognome = r.Cognome,
                Nazionalita = r.Nazionalita
            })
            .ToListAsync();
    }

    public async Task<RegistaPagedResultDTO> GetPagedAsync(int page, int pageSize, string? search)
    {
        var normalizedPage = page < 1 ? 1 : page;
        var normalizedPageSize = pageSize < 1 ? 10 : pageSize;

        var query = _context.Registi.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var likePattern = $"%{search.Trim()}%";
            query = query.Where(r =>
                EF.Functions.Like(r.Nome, likePattern) ||
                EF.Functions.Like(r.Cognome, likePattern) ||
                EF.Functions.Like(r.Nazionalita, likePattern));
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
            .OrderBy(r => r.Id)
            .Skip((normalizedPage - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .Select(r => new RegistaDTO
            {
                Id = r.Id,
                Nome = r.Nome,
                Cognome = r.Cognome,
                Nazionalita = r.Nazionalita
            })
            .ToListAsync();

        return new RegistaPagedResultDTO
        {
            Items = items,
            Page = normalizedPage,
            PageSize = normalizedPageSize,
            TotalCount = totalCount,
            TotalPages = totalPages
        };
    }

    public async Task<RegistaDTO?> GetByIdAsync(int id)
    {
        var regista = await _context.Registi.FindAsync(id);
        if (regista is null) return null;

        return new RegistaDTO
        {
            Id = regista.Id,
            Nome = regista.Nome,
            Cognome = regista.Cognome,
            Nazionalita = regista.Nazionalita
        };
    }

    public async Task<RegistaDTO> CreateAsync(RegistaCreateDTO dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Nome) ||
            string.IsNullOrWhiteSpace(dto.Cognome) ||
            string.IsNullOrWhiteSpace(dto.Nazionalita))
        {
            throw new ArgumentException("Nome, Cognome e Nazionalità sono obbligatori");
        }

        var regista = new Regista
        {
            Nome = dto.Nome,
            Cognome = dto.Cognome,
            Nazionalita = dto.Nazionalita
        };

        _context.Registi.Add(regista);
        await _context.SaveChangesAsync();

        return new RegistaDTO
        {
            Id = regista.Id,
            Nome = regista.Nome,
            Cognome = regista.Cognome,
            Nazionalita = regista.Nazionalita
        };
    }

    public async Task<RegistaDTO?> UpdateAsync(int id, RegistaUpdateDTO dto)
    {
        var regista = await _context.Registi.FindAsync(id);
        if (regista is null) return null;

        if (string.IsNullOrWhiteSpace(dto.Nome) ||
            string.IsNullOrWhiteSpace(dto.Cognome) ||
            string.IsNullOrWhiteSpace(dto.Nazionalita))
        {
            throw new ArgumentException("Nome, Cognome e Nazionalità sono obbligatori");
        }

        regista.Nome = dto.Nome;
        regista.Cognome = dto.Cognome;
        regista.Nazionalita = dto.Nazionalita;

        await _context.SaveChangesAsync();

        return new RegistaDTO
        {
            Id = regista.Id,
            Nome = regista.Nome,
            Cognome = regista.Cognome,
            Nazionalita = regista.Nazionalita
        };
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var regista = await _context.Registi.FindAsync(id);
        if (regista is null) return false;

        _context.Registi.Remove(regista);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<FilmDTO>> GetFilmsByRegistaIdAsync(int id)
    {
        var regista = await _context.Registi
            .Include(r => r.Films)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (regista is null) return new List<FilmDTO>();

        return regista.Films.Select(f => new FilmDTO
        {
            Id = f.Id,
            Titolo = f.Titolo,
            DataProduzione = f.DataProduzione,
            RegistaId = f.RegistaId,
            RegistaNome = regista.Nome,
            RegistaCognome = regista.Cognome,
            Durata = f.Durata,
            CopertinaPath = f.CopertinaPath ?? _defaultCoverPath,
            FilmatoPath = f.FilmatoPath
        }).ToList();
    }
}
