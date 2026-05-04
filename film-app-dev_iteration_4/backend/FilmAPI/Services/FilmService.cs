using FilmAPI.Data;
using FilmAPI.DTO;
using FilmAPI.Model;
using Microsoft.EntityFrameworkCore;

namespace FilmAPI.Services;

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
            .Include(f => f.Regista)
            .Include(f => f.FilmCategorie)
                .ThenInclude(fc => fc.Categoria)
            .Select(f => new FilmDTO
            {
                Id = f.Id,
                Titolo = f.Titolo,
                DataProduzione = f.DataProduzione,
                RegistaId = f.RegistaId,
                RegistaNome = f.Regista != null ? f.Regista.Nome : null,
                RegistaCognome = f.Regista != null ? f.Regista.Cognome : null,
                Durata = f.Durata,
                CopertinaPath = f.CopertinaPath ?? _defaultCoverPath,
                FilmatoPath = f.FilmatoPath,
                DescrizioneLunga = f.DescrizioneLunga,
                CastText = f.CastText,
                DataRilascio = f.DataRilascio,
                VoteAverage = f.VoteAverage,
                Categorie = f.FilmCategorie.Select(fc => new CategoriaDTO
                {
                    Id = fc.Categoria!.Id,
                    Nome = fc.Categoria.Nome
                }).ToList()
            })
            .ToListAsync();
    }

    public async Task<FilmPagedResultDTO> GetPagedAsync(int page, int pageSize, string? search, string? filter = null)
    {
        var normalizedPage = page < 1 ? 1 : page;
        var normalizedPageSize = pageSize < 1 ? 10 : pageSize;

        var query = _context.Films
            .Include(f => f.Regista)
            .Include(f => f.FilmCategorie)
                .ThenInclude(fc => fc.Categoria)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var likePattern = $"%{search.Trim()}%";
            query = query.Where(f =>
                EF.Functions.Like(f.Titolo, likePattern) ||
                (f.Regista != null &&
                    (EF.Functions.Like(f.Regista.Nome + " " + f.Regista.Cognome, likePattern) ||
                     EF.Functions.Like(f.Regista.Nome, likePattern) ||
                     EF.Functions.Like(f.Regista.Cognome, likePattern))));
        }

        // Filtri
        var oggi = DateOnly.FromDateTime(DateTime.Today);
        switch (filter?.ToLowerInvariant())
        {
            case "upcoming":
                query = query.Where(f => f.DataRilascio != null && f.DataRilascio >= oggi);
                break;
            case "now-playing":
                query = query.Where(f => f.Proiezioni.Any(p => p.Data >= DateTime.Today));
                break;
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
            .OrderBy(f => f.Id)
            .Skip((normalizedPage - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .Select(f => new FilmDTO
            {
                Id = f.Id,
                Titolo = f.Titolo,
                DataProduzione = f.DataProduzione,
                RegistaId = f.RegistaId,
                RegistaNome = f.Regista != null ? f.Regista.Nome : null,
                RegistaCognome = f.Regista != null ? f.Regista.Cognome : null,
                Durata = f.Durata,
                CopertinaPath = f.CopertinaPath ?? _defaultCoverPath,
                FilmatoPath = f.FilmatoPath,
                DescrizioneLunga = f.DescrizioneLunga,
                CastText = f.CastText,
                DataRilascio = f.DataRilascio,
                VoteAverage = f.VoteAverage,
                Categorie = f.FilmCategorie.Select(fc => new CategoriaDTO
                {
                    Id = fc.Categoria!.Id,
                    Nome = fc.Categoria.Nome
                }).ToList()
            })
            .ToListAsync();

        return new FilmPagedResultDTO
        {
            Items = items,
            Page = normalizedPage,
            PageSize = normalizedPageSize,
            TotalCount = totalCount,
            TotalPages = totalPages
        };
    }

    public async Task<FilmDTO?> GetByIdAsync(int id)
    {
        var film = await _context.Films
            .Include(f => f.Regista)
            .Include(f => f.FilmCategorie)
                .ThenInclude(fc => fc.Categoria)
            .FirstOrDefaultAsync(f => f.Id == id);

        if (film is null) return null;

        return MapToDTO(film);
    }

    public async Task<FilmDTO> CreateAsync(FilmCreateDTO dto)
    {
        var regista = await _context.Registi.FindAsync(dto.RegistaId);
        if (regista is null)
        {
            throw new ArgumentException("Regista non trovato");
        }

        ValidateFilmatoPath(dto.FilmatoPath);

        var film = new Film
        {
            Titolo = dto.Titolo,
            DataProduzione = dto.DataProduzione,
            RegistaId = dto.RegistaId,
            Durata = dto.Durata,
            CopertinaPath = string.IsNullOrEmpty(dto.CopertinaPath) ? _defaultCoverPath : dto.CopertinaPath,
            FilmatoPath = dto.FilmatoPath,
            DescrizioneLunga = dto.DescrizioneLunga,
            CastText = dto.CastText,
            DataRilascio = dto.DataRilascio
        };

        if (dto.CategorieIds != null && dto.CategorieIds.Count > 0)
        {
            await SyncFilmCategorieAsync(film, dto.CategorieIds);
        }

        _context.Films.Add(film);
        await _context.SaveChangesAsync();

        return await MapToDTOAsync(film, regista);
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

        ValidateFilmatoPath(dto.FilmatoPath);

        film.Titolo = dto.Titolo;
        film.DataProduzione = dto.DataProduzione;
        film.RegistaId = dto.RegistaId;
        film.Durata = dto.Durata;
        film.CopertinaPath = string.IsNullOrEmpty(dto.CopertinaPath) ? _defaultCoverPath : dto.CopertinaPath;
        film.FilmatoPath = dto.FilmatoPath;
        film.DescrizioneLunga = dto.DescrizioneLunga;
        film.CastText = dto.CastText;
        film.DataRilascio = dto.DataRilascio;

        if (dto.CategorieIds != null)
        {
            await SyncFilmCategorieAsync(film, dto.CategorieIds);
        }

        await _context.SaveChangesAsync();

        return await MapToDTOAsync(film, regista);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var film = await _context.Films.FindAsync(id);
        if (film is null) return false;

        _context.Films.Remove(film);
        await _context.SaveChangesAsync();
        return true;
    }

    private static void ValidateFilmatoPath(string? filmatoPath)
    {
        if (string.IsNullOrEmpty(filmatoPath)) return;

        if (!Uri.TryCreate(filmatoPath, UriKind.Absolute, out var uri) ||
            (uri.Scheme != "http" && uri.Scheme != "https"))
        {
            throw new ArgumentException("Il trailer URL deve essere un URL assoluto valido (http/https)");
        }
    }

    private async Task SyncFilmCategorieAsync(Film film, List<int> categorieIds)
    {
        var existing = await _context.FilmCategorie
            .Where(fc => fc.FilmId == film.Id)
            .ToListAsync();

        var existingIds = existing.Select(fc => fc.CategoriaId).ToHashSet();
        var desiredIds = categorieIds.ToHashSet();

        var toRemove = existing.Where(fc => !desiredIds.Contains(fc.CategoriaId)).ToList();
        var toAdd = desiredIds.Where(id => !existingIds.Contains(id)).ToList();

        foreach (var fc in toRemove)
        {
            _context.FilmCategorie.Remove(fc);
        }

        foreach (var catId in toAdd)
        {
            var exists = await _context.Categorie.AnyAsync(c => c.Id == catId);
            if (exists)
            {
                _context.FilmCategorie.Add(new FilmCategoria
                {
                    FilmId = film.Id,
                    CategoriaId = catId
                });
            }
        }
    }

    private FilmDTO MapToDTO(Film film)
    {
        return new FilmDTO
        {
            Id = film.Id,
            Titolo = film.Titolo,
            DataProduzione = film.DataProduzione,
            RegistaId = film.RegistaId,
            RegistaNome = film.Regista?.Nome,
            RegistaCognome = film.Regista?.Cognome,
            Durata = film.Durata,
            CopertinaPath = film.CopertinaPath ?? _defaultCoverPath,
            FilmatoPath = film.FilmatoPath,
            DescrizioneLunga = film.DescrizioneLunga,
            CastText = film.CastText,
            DataRilascio = film.DataRilascio,
            VoteAverage = film.VoteAverage,
            Categorie = film.FilmCategorie.Select(fc => new CategoriaDTO
            {
                Id = fc.Categoria!.Id,
                Nome = fc.Categoria.Nome
            }).ToList()
        };
    }

    private async Task<FilmDTO> MapToDTOAsync(Film film, Regista regista)
    {
        var filmWithCategories = await _context.Films
            .Include(f => f.FilmCategorie)
                .ThenInclude(fc => fc.Categoria)
            .FirstOrDefaultAsync(f => f.Id == film.Id);

        return new FilmDTO
        {
            Id = film.Id,
            Titolo = film.Titolo,
            DataProduzione = film.DataProduzione,
            RegistaId = film.RegistaId,
            RegistaNome = regista.Nome,
            RegistaCognome = regista.Cognome,
            Durata = film.Durata,
            CopertinaPath = film.CopertinaPath,
            FilmatoPath = film.FilmatoPath,
            DescrizioneLunga = film.DescrizioneLunga,
            CastText = film.CastText,
            DataRilascio = film.DataRilascio,
            VoteAverage = film.VoteAverage,
            Categorie = filmWithCategories?.FilmCategorie.Select(fc => new CategoriaDTO
            {
                Id = fc.Categoria!.Id,
                Nome = fc.Categoria.Nome
            }).ToList() ?? new List<CategoriaDTO>()
        };
    }
}
