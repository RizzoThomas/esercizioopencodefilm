using FilmAPI.Data;
using FilmAPI.DTO;
using FilmAPI.Model;
using Microsoft.EntityFrameworkCore;

namespace FilmAPI.Services;

public class ProgrammazioneService : IProgrammazioneService
{
    private readonly FilmDbContext _context;
    private readonly string _defaultCoverPath;

    public ProgrammazioneService(FilmDbContext context)
    {
        _context = context;
        _defaultCoverPath = Environment.GetEnvironmentVariable("DEFAULT_COVER_IMAGE_PATH") ?? "/media/defaults/cover-default.jpg";
    }

    public async Task<ProgrammazioneFilmPagedResultDTO> GetFilmsAsync(string? tab, string? search, int? categoriaId, int? cinemaId, int page = 1, int pageSize = 20)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var nowUtc = DateTime.UtcNow;
        var next7DaysUtc = nowUtc.AddDays(7);
        var today = DateOnly.FromDateTime(nowUtc);
        var todayPlus14 = today.AddDays(14);

        var filmsQuery = _context.Films
            .Include(f => f.FilmCategorie)
                .ThenInclude(fc => fc.Categoria)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var likePattern = $"%{search.Trim()}%";
            filmsQuery = filmsQuery.Where(f => EF.Functions.Like(f.Titolo, likePattern));
        }

        if (categoriaId.HasValue)
        {
            filmsQuery = filmsQuery.Where(f => f.FilmCategorie.Any(fc => fc.CategoriaId == categoriaId.Value));
        }

        var films = await filmsQuery.ToListAsync();

        var filmIds = films.Select(f => f.Id).ToList();

        var showCountsByFilm = await _context.Shows
            .Where(s => filmIds.Contains(s.FilmId) && s.StartAtUtc >= nowUtc && s.StartAtUtc <= next7DaysUtc)
            .GroupBy(s => s.FilmId)
            .Select(g => new { FilmId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.FilmId, g => g.Count);

        var endOfTodayUtc = nowUtc.Date.AddDays(1);
        var hasShowTodayByFilm = await _context.Shows
            .Where(s => filmIds.Contains(s.FilmId) && s.StartAtUtc >= nowUtc.Date && s.StartAtUtc < endOfTodayUtc)
            .Select(s => s.FilmId)
            .Distinct()
            .ToDictionaryAsync(fid => fid, _ => true);

        Dictionary<int, DateTime?> prossimoShowByFilm = new();
        Dictionary<int, bool> disponibileNelCinemaByFilm = new();

        if (cinemaId.HasValue)
        {
            prossimoShowByFilm = await _context.Shows
                .Where(s => filmIds.Contains(s.FilmId) && s.CinemaId == cinemaId.Value && s.StartAtUtc >= nowUtc)
                .GroupBy(s => s.FilmId)
                .Select(g => new { FilmId = g.Key, MinStart = g.Min(s => s.StartAtUtc) })
                .ToDictionaryAsync(g => g.FilmId, g => (DateTime?)g.MinStart);

            disponibileNelCinemaByFilm = await _context.Shows
                .Where(s => filmIds.Contains(s.FilmId) && s.CinemaId == cinemaId.Value && s.StartAtUtc >= nowUtc)
                .Select(s => s.FilmId)
                .Distinct()
                .ToDictionaryAsync(fid => fid, _ => true);
        }

        var results = new List<ProgrammazioneFilmDTO>();

        foreach (var film in films)
        {
            var showCount = showCountsByFilm.GetValueOrDefault(film.Id, 0);
            var isInEvidenza = showCount > 0;
            var hasShowToday = hasShowTodayByFilm.GetValueOrDefault(film.Id, false);
            var availableInSelectedCinema = cinemaId.HasValue && disponibileNelCinemaByFilm.GetValueOrDefault(film.Id, false);
            var isInUscita = film.DataRilascio.HasValue
                && film.DataRilascio.Value >= today
                && film.DataRilascio.Value <= todayPlus14
                && !hasShowToday
                && !availableInSelectedCinema;

            bool includeInTab = tab switch
            {
                "evidenza" => isInEvidenza,
                "uscita" => isInUscita,
                "tutti" => showCount > 0 || (film.DataRilascio.HasValue && film.DataRilascio.Value <= todayPlus14),
                _ => true
            };

            if (!includeInTab) continue;

            var dto = new ProgrammazioneFilmDTO
            {
                Id = film.Id,
                Titolo = film.Titolo,
                CopertinaPath = film.CopertinaPath ?? _defaultCoverPath,
                Durata = film.Durata,
                Categorie = film.FilmCategorie.Select(fc => new CategoriaDTO
                {
                    Id = fc.Categoria!.Id,
                    Nome = fc.Categoria.Nome
                }).ToList(),
                DataRilascio = film.DataRilascio,
                InEvidenza = isInEvidenza,
                InUscita = isInUscita,
                ShowCountNext7Days = showCount,
                DisponibileNelCinemaSelezionato = availableInSelectedCinema,
                ProssimoShowNelCinemaSelezionato = cinemaId.HasValue ? prossimoShowByFilm.GetValueOrDefault(film.Id) : null
            };

            results.Add(dto);
        }

        results = tab switch
        {
            "evidenza" => results
                .OrderByDescending(r => r.ShowCountNext7Days)
                .ThenByDescending(r => r.DisponibileNelCinemaSelezionato)
                .ThenBy(r => r.ProssimoShowNelCinemaSelezionato)
                .ThenBy(r => r.Titolo)
                .ToList(),
            "uscita" => results
                .OrderBy(r => r.DataRilascio)
                .ThenBy(r => r.Titolo)
                .ToList(),
            "tutti" => results
                .OrderBy(r => r.Titolo)
                .ToList(),
            _ => results
        };

        var totalCount = results.Count;
        var pagedItems = results
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);

        return new ProgrammazioneFilmPagedResultDTO
        {
            Items = pagedItems,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages,
            HasNextPage = page < totalPages,
            HasPreviousPage = page > 1 && totalPages > 0
        };
    }

    public async Task<List<CinemaCardDTO>> GetCinemasAsync(double? lat, double? lng)
    {
        var cinemas = await _context.Cinemas
            .Include(c => c.Sale)
            .ToListAsync();

        var results = cinemas.Select(c =>
        {
            var tipologie = c.Sale
                .Where(s => s.IsAttiva)
                .Select(s => s.TipoSala.ToString())
                .Distinct()
                .ToList();

            double? distanza = null;
            if (lat.HasValue && lng.HasValue && c.Latitudine.HasValue && c.Longitudine.HasValue)
            {
                distanza = CalculateDistanceKm(lat.Value, lng.Value, c.Latitudine.Value, c.Longitudine.Value);
            }

            return new CinemaCardDTO
            {
                Id = c.Id,
                Nome = c.Nome,
                Citta = c.Citta,
                Indirizzo = c.Indirizzo,
                TipologieSalePresenti = tipologie,
                DistanzaKm = distanza != null ? Math.Round(distanza.Value, 2) : null,
                Latitudine = c.Latitudine,
                Longitudine = c.Longitudine
            };
        }).ToList();

        if (lat.HasValue && lng.HasValue)
        {
            results = results
                .OrderBy(c => c.DistanzaKm.HasValue ? c.DistanzaKm.Value : double.MaxValue)
                .ThenBy(c => c.Nome)
                .ToList();
        }
        else
        {
            results = results.OrderBy(c => c.Nome).ToList();
        }

        return results;
    }

    public async Task<FilmSchedaDTO?> GetFilmSchedaAsync(int filmId, int? cinemaId)
    {
        var film = await _context.Films
            .Include(f => f.Regista)
            .Include(f => f.FilmCategorie)
                .ThenInclude(fc => fc.Categoria)
            .FirstOrDefaultAsync(f => f.Id == filmId);

        if (film is null) return null;

        var castList = string.IsNullOrWhiteSpace(film.CastText)
            ? new List<string>()
            : film.CastText.Split(new[] { '\r', '\n', ',' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();

        var dto = new FilmSchedaDTO
        {
            Id = film.Id,
            Titolo = film.Titolo,
            CopertinaPath = film.CopertinaPath ?? _defaultCoverPath,
            Durata = film.Durata,
            DataProduzione = film.DataProduzione,
            DataRilascio = film.DataRilascio,
            DescrizioneLunga = film.DescrizioneLunga,
            CastText = film.CastText,
            CastList = castList,
            Categorie = film.FilmCategorie.Select(fc => new CategoriaDTO
            {
                Id = fc.Categoria!.Id,
                Nome = fc.Categoria.Nome
            }).ToList(),
            RegistaNome = film.Regista?.Nome,
            RegistaCognome = film.Regista?.Cognome
        };

        if (cinemaId.HasValue)
        {
            var cinema = await _context.Cinemas.FindAsync(cinemaId.Value);
            if (cinema is not null)
            {
                dto.CinemaSelezionato = new CinemaSintesiDTO
                {
                    Id = cinema.Id,
                    Nome = cinema.Nome,
                    Citta = cinema.Citta,
                    Indirizzo = cinema.Indirizzo,
                    Telefono = cinema.Telefono,
                    CodiceLocale = cinema.CodiceLocale
                };
            }
        }

        var nowUtc = DateTime.UtcNow;
        var shows = await _context.Shows
            .Include(s => s.Sala)
            .Where(s => s.FilmId == filmId && s.StartAtUtc >= nowUtc)
            .OrderBy(s => s.StartAtUtc)
            .ThenBy(s => s.Sala!.TipoSala)
            .ToListAsync();

        if (cinemaId.HasValue)
        {
            shows = shows.Where(s => s.CinemaId == cinemaId.Value).ToList();
        }

        var groupedByDate = shows
            .GroupBy(s => DateOnly.FromDateTime(s.StartAtUtc))
            .OrderBy(g => g.Key)
            .ToList();

        foreach (var dateGroup in groupedByDate)
        {
            var dateDto = new FilmSchedaShowGroupDTO
            {
                Data = dateGroup.Key
            };

            var groupedByTipoSala = dateGroup
                .GroupBy(s => s.Sala!.TipoSala.ToString())
                .OrderBy(g => g.Key)
                .ToList();

            foreach (var tipoGroup in groupedByTipoSala)
            {
                var tipoDto = new FilmSchedaTipoSalaGroupDTO
                {
                    TipoSala = tipoGroup.Key,
                    Shows = tipoGroup
                        .OrderBy(s => s.StartAtUtc)
                        .ThenBy(s => s.Sala!.NumeroProgressivo)
                        .Select(s => new FilmSchedaShowItemDTO
                        {
                            ShowId = s.Id,
                            StartAtUtc = s.StartAtUtc,
                            PrezzoBase = s.PrezzoBase,
                            SupplementoSala = s.SupplementoSala,
                            SalaId = s.SalaId,
                            SalaNome = s.Sala!.Nome,
                            SalaNumeroProgressivo = s.Sala.NumeroProgressivo
                        })
                        .ToList()
                };

                dateDto.GruppiPerTipoSala.Add(tipoDto);
            }

            dto.ShowCalendar.Add(dateDto);
        }

        return dto;
    }

    public async Task<List<CinemaCardDTO>> GetMyCinemasAsync()
    {
        var cinemas = await _context.Cinemas
            .Include(c => c.Sale)
            .OrderBy(c => c.Nome)
            .ToListAsync();

        return cinemas.Select(c =>
        {
            var tipologie = c.Sale
                .Where(s => s.IsAttiva)
                .Select(s => s.TipoSala.ToString())
                .Distinct()
                .ToList();

            return new CinemaCardDTO
            {
                Id = c.Id,
                Nome = c.Nome,
                Citta = c.Citta,
                Indirizzo = c.Indirizzo,
                TipologieSalePresenti = tipologie,
                Latitudine = c.Latitudine,
                Longitudine = c.Longitudine
            };
        }).ToList();
    }

    public async Task<CinemaScheduleDayDTO?> GetCinemaScheduleAsync(int cinemaId, DateOnly? date)
    {
        var cinema = await _context.Cinemas.FindAsync(cinemaId);
        if (cinema is null) return null;

        var targetDate = date ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var dayStartUtc = targetDate.ToDateTime(TimeOnly.MinValue).ToUniversalTime();
        var dayEndUtc = targetDate.AddDays(1).ToDateTime(TimeOnly.MinValue).ToUniversalTime();

        var shows = await _context.Shows
            .Include(s => s.Film)
            .Include(s => s.Sala)
            .Where(s => s.CinemaId == cinemaId && s.StartAtUtc >= dayStartUtc && s.StartAtUtc < dayEndUtc)
            .OrderBy(s => s.StartAtUtc)
            .ToListAsync();

        var films = shows
            .GroupBy(s => s.FilmId)
            .Select(g => g.First())
            .Select(s => s.Film!)
            .ToList();

        var scheduleFilms = new List<CinemaScheduleFilmDTO>();

        foreach (var film in films.OrderBy(f => f.Titolo))
        {
            var filmShows = shows.Where(s => s.FilmId == film.Id).ToList();
            var descriptionExtract = string.IsNullOrWhiteSpace(film.DescrizioneLunga)
                ? null
                : film.DescrizioneLunga.Length > 200
                    ? film.DescrizioneLunga.Substring(0, 200) + "..."
                    : film.DescrizioneLunga;

            var filmDto = new CinemaScheduleFilmDTO
            {
                FilmId = film.Id,
                Titolo = film.Titolo,
                CopertinaPath = film.CopertinaPath ?? _defaultCoverPath,
                DescrizioneEstratto = descriptionExtract
            };

            var groupedByTipoSala = filmShows
                .GroupBy(s => s.Sala!.TipoSala.ToString())
                .OrderBy(g => g.Key)
                .ToList();

            foreach (var tipoGroup in groupedByTipoSala)
            {
                var tipoDto = new CinemaScheduleTipoSalaGroupDTO
                {
                    TipoSala = tipoGroup.Key,
                    Shows = tipoGroup
                        .OrderBy(s => s.StartAtUtc)
                        .ThenBy(s => s.Sala!.NumeroProgressivo)
                        .Select(s => new CinemaScheduleShowItemDTO
                        {
                            ShowId = s.Id,
                            StartAtUtc = s.StartAtUtc,
                            SalaId = s.SalaId,
                            SalaNome = s.Sala!.Nome,
                            SalaNumeroProgressivo = s.Sala.NumeroProgressivo
                        })
                        .ToList()
                };

                filmDto.GruppiPerTipoSala.Add(tipoDto);
            }

            scheduleFilms.Add(filmDto);
        }

        return new CinemaScheduleDayDTO
        {
            Cinema = new CinemaSintesiDTO
            {
                Id = cinema.Id,
                Nome = cinema.Nome,
                Citta = cinema.Citta,
                Indirizzo = cinema.Indirizzo,
                Telefono = cinema.Telefono,
                CodiceLocale = cinema.CodiceLocale
            },
            Data = targetDate,
            Films = scheduleFilms
        };
    }

    public async Task<CinemaPreferitoDTO?> GetCinemaPreferitoAsync(int userId)
    {
        var user = await _context.Users
            .Include(u => u.CinemaPreferito)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user is null) return null;

        if (user.CinemaPreferito is null)
        {
            return new CinemaPreferitoDTO { CinemaId = null, Cinema = null };
        }

        return new CinemaPreferitoDTO
        {
            CinemaId = user.CinemaPreferitoId,
            Cinema = new CinemaSintesiDTO
            {
                Id = user.CinemaPreferito.Id,
                Nome = user.CinemaPreferito.Nome,
                Citta = user.CinemaPreferito.Citta,
                Indirizzo = user.CinemaPreferito.Indirizzo,
                Telefono = user.CinemaPreferito.Telefono,
                CodiceLocale = user.CinemaPreferito.CodiceLocale
            }
        };
    }

    public async Task<CinemaPreferitoDTO> SetCinemaPreferitoAsync(int userId, int? cinemaId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user is null) throw new InvalidOperationException("Utente non trovato");

        if (cinemaId.HasValue)
        {
            var cinemaExists = await _context.Cinemas.AnyAsync(c => c.Id == cinemaId.Value);
            if (!cinemaExists) throw new ArgumentException("Cinema non trovato");
        }

        user.CinemaPreferitoId = cinemaId;
        await _context.SaveChangesAsync();

        return await GetCinemaPreferitoAsync(userId) ?? new CinemaPreferitoDTO { CinemaId = null, Cinema = null };
    }

    private static double CalculateDistanceKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371.0;
        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180.0;
}
