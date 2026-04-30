using System.Text;
using FilmAPI.Data;
using FilmAPI.Model;
using Microsoft.EntityFrameworkCore;

namespace FilmApiSeeder;

internal static class Program
{
    private const int MinimumMovies = 50;
    private const int SeedDays = 7;
    private static readonly TimeZoneInfo RomeTimeZone = ResolveRomeTimeZone();

    public static async Task<int> Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        try
        {
            var cancellationToken = CancellationToken.None;
            var options = ParseOptions(args);
            if (options.ShowHelp)
            {
                PrintHelp();
                return 0;
            }

            if ((options.ResetShows || options.ResetAll) && !options.Force)
            {
                throw new InvalidOperationException(
                    "Le operazioni di reset richiedono conferma esplicita. Riesegui con --force insieme a --reset-shows oppure --reset-all.");
            }

            var repoRoot = FindRepositoryRoot();
            LoadEnvFiles(repoRoot);

            var tmdbBearerToken = Environment.GetEnvironmentVariable("TMDB_BEARER_TOKEN");
            if (string.IsNullOrWhiteSpace(tmdbBearerToken))
            {
                throw new InvalidOperationException(
                    "Variabile ambiente TMDB_BEARER_TOKEN non valorizzata. Inserisci il bearer token in backend/.env o nelle variabili di ambiente.");
            }

            await using var dbContext = CreateDbContext();
            await dbContext.Database.MigrateAsync(cancellationToken);

            if (options.ResetAll)
            {
                Console.WriteLine("Reset completo seed in corso...");
                await ResetAllAsync(dbContext, cancellationToken);
            }
            else if (options.ResetShows)
            {
                Console.WriteLine("Reset programmazione in corso...");
                await ResetShowsAsync(dbContext, cancellationToken);
            }

            using var httpClient = new HttpClient();
            var tmdb = new TmdbClient(httpClient, tmdbBearerToken);

            Console.WriteLine("Seeding categorie...");
            var categorieByName = await EnsureCategoriesAsync(dbContext, cancellationToken);

            Console.WriteLine("Recupero catalogo film da TMDB...");
            var movieDetails = await LoadMovieDetailsAsync(tmdb, cancellationToken);
            if (movieDetails.Count < MinimumMovies)
            {
                throw new InvalidOperationException($"TMDB ha restituito solo {movieDetails.Count} film validi; ne servono almeno {MinimumMovies}.");
            }

            Console.WriteLine("Aggiornamento film e registi...");
            var seededFilms = await UpsertFilmsAsync(dbContext, tmdb, movieDetails, categorieByName, cancellationToken);

            Console.WriteLine("Aggiornamento cinema e sale...");
            var saleByCinemaCode = await UpsertCinemasAndSaleAsync(dbContext, cancellationToken);

            Console.WriteLine("Generazione show e programmazione...");
            await UpsertShowsAsync(dbContext, seededFilms, saleByCinemaCode, cancellationToken);

            Console.WriteLine($"Completato: {seededFilms.Count} film, {saleByCinemaCode.Count} cinema, {saleByCinemaCode.Sum(kvp => kvp.Value.Count)} sale.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }

    private static FilmDbContext CreateDbContext()
    {
        var dbHost = Environment.GetEnvironmentVariable("DB_HOST") ?? "localhost";
        var dbPort = Environment.GetEnvironmentVariable("DB_PORT") ?? "3306";
        var dbName = Environment.GetEnvironmentVariable("DB_NAME") ?? "film-api-db";
        var dbUser = Environment.GetEnvironmentVariable("DB_USER") ?? "root";
        var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "root";
        var dbUseAutoDetect = (Environment.GetEnvironmentVariable("DB_USE_AUTODETECT") ?? "true")
            .Equals("true", StringComparison.OrdinalIgnoreCase);
        var dbServerVersion = Environment.GetEnvironmentVariable("DB_SERVER_VERSION") ?? "10.11.0-mariadb";

        var connectionString = $"Server={dbHost};Port={dbPort};Database={dbName};User Id={dbUser};Password={dbPassword};";
        var serverVersion = dbUseAutoDetect
            ? ServerVersion.AutoDetect(connectionString)
            : ServerVersion.Parse(dbServerVersion);

        var options = new DbContextOptionsBuilder<FilmDbContext>()
            .UseMySql(connectionString, serverVersion)
            .EnableDetailedErrors()
            .Options;

        return new FilmDbContext(options);
    }

    private static async Task<Dictionary<string, Categoria>> EnsureCategoriesAsync(FilmDbContext dbContext, CancellationToken cancellationToken)
    {
        var existing = await dbContext.Categorie.ToListAsync(cancellationToken);
        foreach (var nome in SeedCatalog.CategoriaNames)
        {
            if (existing.Any(c => string.Equals(c.Nome, nome, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            var categoria = new Categoria { Nome = nome };
            dbContext.Categorie.Add(categoria);
            existing.Add(categoria);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return existing.ToDictionary(c => c.Nome, StringComparer.OrdinalIgnoreCase);
    }

    private static async Task<List<TmdbMovieDetails>> LoadMovieDetailsAsync(TmdbClient tmdb, CancellationToken cancellationToken)
    {
        var movies = new List<TmdbMovieDetails>();
        var seenIds = new HashSet<int>();

        foreach (var target in SeedCatalog.MovieTargets)
        {
            var details = await tmdb.SearchMovieWithDetailsAsync(target, cancellationToken);
            if (details is null || !seenIds.Add(details.Id))
            {
                continue;
            }

            if (!IsUsableMovie(details))
            {
                continue;
            }

            movies.Add(details);
        }

        if (movies.Count >= MinimumMovies)
        {
            return movies;
        }

        foreach (var genreId in SeedCatalog.FallbackDiscoverGenres)
        {
            for (var page = 1; page <= 3 && movies.Count < MinimumMovies; page++)
            {
                var movieIds = await tmdb.DiscoverMovieIdsByGenreAsync(genreId, page, cancellationToken);
                foreach (var movieId in movieIds)
                {
                    if (!seenIds.Add(movieId))
                    {
                        continue;
                    }

                    var details = await tmdb.GetMovieDetailsAsync(movieId, cancellationToken);
                    if (details is null || !IsUsableMovie(details))
                    {
                        continue;
                    }

                    movies.Add(details);
                    if (movies.Count >= MinimumMovies)
                    {
                        return movies;
                    }
                }
            }
        }

        return movies;
    }

    private static bool IsUsableMovie(TmdbMovieDetails details)
    {
        if (string.IsNullOrWhiteSpace(details.Title) || details.Runtime is null || details.Runtime <= 0)
        {
            return false;
        }

        if (details.ReleaseDate is null)
        {
            return false;
        }

        return details.Credits?.Crew?.Any(c =>
            string.Equals(c.Job, "Director", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(c.Department, "Directing", StringComparison.OrdinalIgnoreCase)) == true;
    }

    private static async Task<List<SeededFilm>> UpsertFilmsAsync(
        FilmDbContext dbContext,
        TmdbClient tmdb,
        IReadOnlyList<TmdbMovieDetails> movieDetails,
        IReadOnlyDictionary<string, Categoria> categorieByName,
        CancellationToken cancellationToken)
    {
        var registi = await dbContext.Registi.ToListAsync(cancellationToken);
        var films = await dbContext.Films
            .Include(f => f.FilmCategorie)
            .ToListAsync(cancellationToken);

        var seededFilms = new List<SeededFilm>();

        foreach (var details in movieDetails)
        {
            var directorCredit = details.Credits!.Crew
                .FirstOrDefault(c => string.Equals(c.Job, "Director", StringComparison.OrdinalIgnoreCase))
                ?? details.Credits.Crew.First(c => string.Equals(c.Department, "Directing", StringComparison.OrdinalIgnoreCase));

            var director = await GetOrCreateDirectorAsync(dbContext, tmdb, registi, directorCredit, cancellationToken);
            var film = FindExistingFilm(films, details);
            if (film is null)
            {
                film = new Film();
                dbContext.Films.Add(film);
                films.Add(film);
            }

            var releaseDate = details.ReleaseDate!.Value;
            film.Titolo = details.Title.Trim();
            film.RegistaId = director.Id;
            film.DataProduzione = DateTime.SpecifyKind(releaseDate.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
            film.DataRilascio = releaseDate;
            film.Durata = details.Runtime!.Value;
            film.DescrizioneLunga = Truncate(details.Overview, 2000);
            film.CastText = BuildCastText(details.Credits.Cast);
            film.CopertinaPath = details.PosterFullUrl;
            film.FilmatoPath = null;

            await dbContext.SaveChangesAsync(cancellationToken);

            var categoriaNames = details.Genres
                .Select(g => SeedCatalog.TmdbGenreToCategoria.TryGetValue(g.Id, out var categoriaName) ? categoriaName : null)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (categoriaNames.Count == 0)
            {
                categoriaNames.Add("Drammatico");
            }

            var existingCategoriaIds = film.FilmCategorie.Select(fc => fc.CategoriaId).ToHashSet();
            foreach (var categoriaName in categoriaNames)
            {
                var categoria = categorieByName[categoriaName!];
                if (existingCategoriaIds.Add(categoria.Id))
                {
                    film.FilmCategorie.Add(new FilmCategoria
                    {
                        FilmId = film.Id,
                        CategoriaId = categoria.Id
                    });
                }
            }

            seededFilms.Add(new SeededFilm(film, categoriaNames!));
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return seededFilms;
    }

    private static async Task<Regista> GetOrCreateDirectorAsync(
        FilmDbContext dbContext,
        TmdbClient tmdb,
        List<Regista> registi,
        TmdbCrewMember directorCredit,
        CancellationToken cancellationToken)
    {
        var normalizedName = NormalizeKey(directorCredit.Name);
        var existing = registi.FirstOrDefault(r => NormalizeKey($"{r.Nome} {r.Cognome}") == normalizedName);
        if (existing is not null)
        {
            return existing;
        }

        var person = await tmdb.GetPersonDetailsAsync(directorCredit.Id, cancellationToken);
        var (nome, cognome) = SplitName(person?.Name ?? directorCredit.Name);
        var regista = new Regista
        {
            Nome = Truncate(nome, 100)!,
            Cognome = Truncate(cognome, 100)!,
            Nazionalita = Truncate(ResolveNationality(person?.PlaceOfBirth), 100)!
        };

        dbContext.Registi.Add(regista);
        registi.Add(regista);
        await dbContext.SaveChangesAsync(cancellationToken);
        return regista;
    }

    private static Film? FindExistingFilm(List<Film> films, TmdbMovieDetails details)
    {
        var detailKeys = new[]
        {
            NormalizeKey(details.Title),
            NormalizeKey(details.OriginalTitle)
        };

        return films.FirstOrDefault(f => detailKeys.Contains(NormalizeKey(f.Titolo)));
    }

    private static string BuildCastText(IEnumerable<TmdbCastMember> cast)
    {
        return string.Join(", ", cast
            .OrderBy(c => c.Order)
            .Take(8)
            .Select(c => c.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name)));
    }

    private static async Task<Dictionary<string, List<Sala>>> UpsertCinemasAndSaleAsync(FilmDbContext dbContext, CancellationToken cancellationToken)
    {
        var cinemas = await dbContext.Cinemas
            .Include(c => c.Sale)
            .ThenInclude(s => s.Posti)
            .ToListAsync(cancellationToken);

        var result = new Dictionary<string, List<Sala>>(StringComparer.OrdinalIgnoreCase);

        foreach (var cinemaSeed in SeedCatalog.Cinemas)
        {
            var cinema = cinemas.FirstOrDefault(c => string.Equals(c.CodiceLocale, cinemaSeed.CodiceLocale, StringComparison.OrdinalIgnoreCase));
            if (cinema is null)
            {
                cinema = new Cinema();
                dbContext.Cinemas.Add(cinema);
                cinemas.Add(cinema);
            }

            cinema.CodiceLocale = cinemaSeed.CodiceLocale;
            cinema.Nome = cinemaSeed.Nome;
            cinema.Citta = cinemaSeed.Citta;
            cinema.Indirizzo = cinemaSeed.Indirizzo;
            cinema.Latitudine = cinemaSeed.Latitudine;
            cinema.Longitudine = cinemaSeed.Longitudine;
            cinema.Telefono = cinemaSeed.Telefono;

            await dbContext.SaveChangesAsync(cancellationToken);

            var sale = BuildSalaSeeds(cinemaSeed)
                .Select(seed => UpsertSala(cinema, seed))
                .ToList();

            await dbContext.SaveChangesAsync(cancellationToken);

            foreach (var sala in sale)
            {
                if (sala.Posti.Any())
                {
                    continue;
                }

                var posti = BuildSeatLayout(sala.TipoSala)
                    .Select(p => new SalaPosto
                    {
                        SalaId = sala.Id,
                        Settore = p.Settore,
                        Fila = p.Fila,
                        Numero = p.Numero,
                        PosX = p.PosX,
                        PosY = p.PosY,
                        IsWheelchair = p.IsWheelchair,
                        IsAttivo = true
                    })
                    .ToList();

                dbContext.SalaPosti.AddRange(posti);
                sala.Posti = posti;
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            result[cinemaSeed.CodiceLocale] = sale;
        }

        return result;

        Sala UpsertSala(Cinema cinema, SalaSeed salaSeed)
        {
            var sala = cinema.Sale.FirstOrDefault(s => s.NumeroProgressivo == salaSeed.NumeroProgressivo);
            if (sala is null)
            {
                sala = new Sala
                {
                    CinemaId = cinema.Id
                };
                cinema.Sale.Add(sala);
            }

            sala.NumeroProgressivo = salaSeed.NumeroProgressivo;
            sala.Nome = salaSeed.Nome;
            sala.TipoSala = salaSeed.TipoSala;
            sala.Supplemento = salaSeed.Supplemento;
            sala.IsAttiva = true;
            return sala;
        }
    }

    private static async Task UpsertShowsAsync(
        FilmDbContext dbContext,
        IReadOnlyList<SeededFilm> seededFilms,
        IReadOnlyDictionary<string, List<Sala>> saleByCinemaCode,
        CancellationToken cancellationToken)
    {
        var cinemas = await dbContext.Cinemas.AsNoTracking().ToListAsync(cancellationToken);
        var existingShows = await dbContext.Shows.ToListAsync(cancellationToken);
        var rawDefaultTicketPrice = Environment.GetEnvironmentVariable("DEFAULT_TICKET_PRICE");
        var defaultTicketPrice = !string.IsNullOrWhiteSpace(rawDefaultTicketPrice)
            && (decimal.TryParse(rawDefaultTicketPrice, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var parsed)
                || decimal.TryParse(rawDefaultTicketPrice, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.GetCultureInfo("it-IT"), out parsed)
                || decimal.TryParse(rawDefaultTicketPrice, out parsed))
            ? parsed
            : 8.50m;

        for (var cinemaIndex = 0; cinemaIndex < SeedCatalog.Cinemas.Count; cinemaIndex++)
        {
            var cinemaSeed = SeedCatalog.Cinemas[cinemaIndex];
            var cinema = cinemas.First(c => string.Equals(c.CodiceLocale, cinemaSeed.CodiceLocale, StringComparison.OrdinalIgnoreCase));
            var sale = saleByCinemaCode[cinemaSeed.CodiceLocale];

            for (var dayOffset = 0; dayOffset < SeedDays; dayOffset++)
            {
                var date = DateOnly.FromDateTime(DateTime.Today).AddDays(dayOffset);
                foreach (var sala in sale.Where(s => s.IsAttiva))
                {
                    var slots = GetDailySlots(date, sala.TipoSala);
                    for (var slotIndex = 0; slotIndex < slots.Count; slotIndex++)
                    {
                        var film = PickFilm(seededFilms, sala.TipoSala, cinemaIndex, dayOffset, slotIndex);
                        var startAtUtc = ConvertRomeLocalToUtc(date, slots[slotIndex]);

                        var existing = existingShows.FirstOrDefault(s =>
                            s.CinemaId == cinema.Id &&
                            s.SalaId == sala.Id &&
                            s.StartAtUtc == startAtUtc);

                        if (existing is null)
                        {
                            existing = new Show();
                            dbContext.Shows.Add(existing);
                            existingShows.Add(existing);
                        }

                        existing.CinemaId = cinema.Id;
                        existing.SalaId = sala.Id;
                        existing.FilmId = film.Film.Id;
                        existing.StartAtUtc = startAtUtc;
                        existing.DurataMinutiSnapshot = film.Film.Durata;
                        existing.PrezzoBase = defaultTicketPrice;
                        existing.SupplementoSala = sala.Supplemento;
                    }
                }
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task ResetShowsAsync(FilmDbContext dbContext, CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var movimenti = await dbContext.MovimentiCredito.ToListAsync(cancellationToken);
        if (movimenti.Count > 0)
        {
            dbContext.MovimentiCredito.RemoveRange(movimenti);
        }

        var biglietti = await dbContext.Biglietti.ToListAsync(cancellationToken);
        if (biglietti.Count > 0)
        {
            dbContext.Biglietti.RemoveRange(biglietti);
        }

        var seatStates = await dbContext.ShowPostiStato.ToListAsync(cancellationToken);
        if (seatStates.Count > 0)
        {
            dbContext.ShowPostiStato.RemoveRange(seatStates);
        }

        var ordini = await dbContext.Ordini.ToListAsync(cancellationToken);
        if (ordini.Count > 0)
        {
            dbContext.Ordini.RemoveRange(ordini);
        }

        var shows = await dbContext.Shows.ToListAsync(cancellationToken);
        if (shows.Count > 0)
        {
            dbContext.Shows.RemoveRange(shows);
        }

        var proiezioni = await dbContext.Proiezioni.ToListAsync(cancellationToken);
        if (proiezioni.Count > 0)
        {
            dbContext.Proiezioni.RemoveRange(proiezioni);
        }

        var prenotazioni = await dbContext.Prenotazioni.ToListAsync(cancellationToken);
        if (prenotazioni.Count > 0)
        {
            dbContext.Prenotazioni.RemoveRange(prenotazioni);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private static async Task ResetAllAsync(FilmDbContext dbContext, CancellationToken cancellationToken)
    {
        await ResetShowsAsync(dbContext, cancellationToken);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var salaPosti = await dbContext.SalaPosti.ToListAsync(cancellationToken);
        if (salaPosti.Count > 0)
        {
            dbContext.SalaPosti.RemoveRange(salaPosti);
        }

        var sale = await dbContext.Sale.ToListAsync(cancellationToken);
        if (sale.Count > 0)
        {
            dbContext.Sale.RemoveRange(sale);
        }

        var filmCategorie = await dbContext.FilmCategorie.ToListAsync(cancellationToken);
        if (filmCategorie.Count > 0)
        {
            dbContext.FilmCategorie.RemoveRange(filmCategorie);
        }

        var films = await dbContext.Films.ToListAsync(cancellationToken);
        if (films.Count > 0)
        {
            dbContext.Films.RemoveRange(films);
        }

        var registi = await dbContext.Registi.ToListAsync(cancellationToken);
        if (registi.Count > 0)
        {
            dbContext.Registi.RemoveRange(registi);
        }

        var usersWithCinemaPreferito = await dbContext.Users
            .Where(u => u.CinemaPreferitoId != null)
            .ToListAsync(cancellationToken);
        foreach (var user in usersWithCinemaPreferito)
        {
            user.CinemaPreferitoId = null;
        }

        var cinemas = await dbContext.Cinemas.ToListAsync(cancellationToken);
        if (cinemas.Count > 0)
        {
            dbContext.Cinemas.RemoveRange(cinemas);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private static List<TimeOnly> GetDailySlots(DateOnly date, TipoSala tipoSala)
    {
        var weekend = date.DayOfWeek is DayOfWeek.Friday or DayOfWeek.Saturday or DayOfWeek.Sunday;
        return tipoSala switch
        {
            TipoSala.ISENSE or TipoSala.XL => weekend
                ? [new TimeOnly(10, 30), new TimeOnly(14, 15), new TimeOnly(18, 0), new TimeOnly(21, 45)]
                : [new TimeOnly(15, 30), new TimeOnly(19, 15), new TimeOnly(22, 45)],
            TipoSala.TreD => weekend
                ? [new TimeOnly(11, 0), new TimeOnly(14, 45), new TimeOnly(18, 30), new TimeOnly(22, 0)]
                : [new TimeOnly(16, 0), new TimeOnly(19, 45), new TimeOnly(23, 0)],
            _ => weekend
                ? [new TimeOnly(10, 45), new TimeOnly(14, 30), new TimeOnly(18, 15), new TimeOnly(21, 30)]
                : [new TimeOnly(15, 15), new TimeOnly(19, 0), new TimeOnly(22, 15)]
        };
    }

    private static SeededFilm PickFilm(IReadOnlyList<SeededFilm> seededFilms, TipoSala tipoSala, int cinemaIndex, int dayOffset, int slotIndex)
    {
        var preferredCategories = tipoSala switch
        {
            TipoSala.TreD => new[] { "Azione", "Fantascienza", "Avventura", "Fantasy", "Animazione" },
            TipoSala.XL or TipoSala.ISENSE => new[] { "Azione", "Fantascienza", "Avventura", "Fantasy", "Storico" },
            _ => new[] { "Drammatico", "Commedia", "Thriller", "Romantico", "Documentario", "Storico", "Azione" }
        };

        var pool = seededFilms
            .Where(f => f.Categorie.Any(c => preferredCategories.Contains(c, StringComparer.OrdinalIgnoreCase)))
            .OrderByDescending(f => f.Film.DataRilascio)
            .ThenBy(f => f.Film.Titolo)
            .ToList();

        if (pool.Count == 0)
        {
            pool = seededFilms.OrderByDescending(f => f.Film.DataRilascio).ThenBy(f => f.Film.Titolo).ToList();
        }

        var index = Math.Abs((cinemaIndex * 97) + (dayOffset * 29) + (slotIndex * 11) + ((int)tipoSala * 17)) % pool.Count;
        return pool[index];
    }

    private static IReadOnlyList<SalaSeed> BuildSalaSeeds(CinemaSeed cinemaSeed)
    {
        var seeds = new List<SalaSeed>
        {
            new(1, TipoSala.DueD, "Sala 1", SeedCatalog.SupplementiBySala[TipoSala.DueD]),
            new(2, TipoSala.TreD, "Sala 2", SeedCatalog.SupplementiBySala[TipoSala.TreD]),
            new(3, TipoSala.DueD, "Sala 3", SeedCatalog.SupplementiBySala[TipoSala.DueD])
        };

        if (cinemaSeed.HasXl)
        {
            seeds.Add(new(seeds.Count + 1, TipoSala.XL, "Sala XL", SeedCatalog.SupplementiBySala[TipoSala.XL]));
        }

        if (cinemaSeed.HasIsense)
        {
            seeds.Add(new(seeds.Count + 1, TipoSala.ISENSE, "Sala ISENSE", SeedCatalog.SupplementiBySala[TipoSala.ISENSE]));
        }

        while (seeds.Count < cinemaSeed.NumeroSale)
        {
            var nextIndex = seeds.Count + 1;
            seeds.Add(new(nextIndex, nextIndex % 2 == 0 ? TipoSala.TreD : TipoSala.DueD, $"Sala {nextIndex}", nextIndex % 2 == 0 ? SeedCatalog.SupplementiBySala[TipoSala.TreD] : 0m));
        }

        return seeds.OrderBy(s => s.NumeroProgressivo).ToList();
    }

    private static List<SeatSeed> BuildSeatLayout(TipoSala tipoSala)
    {
        var layout = new List<SeatSeed>();
        var profile = tipoSala switch
        {
            TipoSala.ISENSE => new LayoutProfile(18, 5, 24, 5, 14),
            TipoSala.XL => new LayoutProfile(17, 5, 22, 5, 14),
            TipoSala.TreD => new LayoutProfile(15, 4, 20, 4, 12),
            _ => new LayoutProfile(14, 4, 18, 4, 10)
        };

        for (var fila = 1; fila <= profile.TotalRows; fila++)
        {
            if (fila <= 5)
            {
                AddSection("PLATEA-SX", fila, profile.SideSeats, 1, fila);
                AddSection("PLATEA-CENTRO", fila, profile.CenterSeats, 7, fila);
                AddSection("PLATEA-DX", fila, profile.SideSeats, 7 + profile.CenterSeats + 3, fila);
                continue;
            }

            if (fila <= profile.TotalRows - 2)
            {
                AddSection("GALLERIA-SX", fila, Math.Max(2, profile.SideSeats - 1), 3, fila + 1);
                AddSection("GALLERIA-CENTRO", fila, profile.CenterSeats - 2, 8, fila + 1);
                AddSection("GALLERIA-DX", fila, Math.Max(2, profile.SideSeats - 1), 9 + profile.CenterSeats, fila + 1);
                continue;
            }

            AddSection("VIP", fila, profile.VipSeats, 10, fila + 2);
        }

        var wheelchairRow = profile.TotalRows;
        layout.Add(new SeatSeed("ACCESS-SX", wheelchairRow, 1, 5, wheelchairRow + 3, true));
        layout.Add(new SeatSeed("ACCESS-DX", wheelchairRow, 1, 10 + profile.VipSeats + 2, wheelchairRow + 3, true));

        return layout;

        void AddSection(string settore, int fila, int seats, int startX, int posY)
        {
            for (var posto = 1; posto <= seats; posto++)
            {
                layout.Add(new SeatSeed(settore, fila, posto, startX + posto - 1, posY, false));
            }
        }
    }

    private static DateTime ConvertRomeLocalToUtc(DateOnly date, TimeOnly time)
    {
        var local = date.ToDateTime(time, DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(local, RomeTimeZone);
    }

    private static string ResolveNationality(string? placeOfBirth)
    {
        if (string.IsNullOrWhiteSpace(placeOfBirth))
        {
            return "Internazionale";
        }

        var country = placeOfBirth.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
        if (string.IsNullOrWhiteSpace(country))
        {
            return "Internazionale";
        }

        return country.Trim() switch
        {
            "USA" or "United States" or "United States of America" => "Stati Uniti",
            "UK" or "United Kingdom" or "England" or "Scotland" or "Wales" => "Regno Unito",
            "South Korea" => "Corea del Sud",
            "New Zealand" => "Nuova Zelanda",
            "Japan" => "Giappone",
            "France" => "Francia",
            "Germany" => "Germania",
            "Italy" => "Italia",
            "Spain" => "Spagna",
            "Mexico" => "Messico",
            "Canada" => "Canada",
            "Australia" => "Australia",
            _ => country.Trim()
        };
    }

    private static (string Nome, string Cognome) SplitName(string fullName)
    {
        var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
        {
            return ("Nome", "Sconosciuto");
        }

        if (parts.Length == 1)
        {
            return (parts[0], "Sconosciuto");
        }

        return (string.Join(' ', parts[..^1]), parts[^1]);
    }

    private static string NormalizeKey(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return new string(value.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());
    }

    private static string? Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        return value.Length <= maxLength ? value : value[..maxLength];
    }

    private static TimeZoneInfo ResolveRomeTimeZone()
    {
        foreach (var id in new[] { "Europe/Rome", "W. Europe Standard Time" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException)
            {
            }
        }

        return TimeZoneInfo.Local;
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var solutionPath = Path.Combine(directory.FullName, "claude-code-test.sln");
            if (File.Exists(solutionPath))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Repository root non trovata.");
    }

    private static void LoadEnvFiles(string repoRoot)
    {
        var envFiles = new[]
        {
            Path.Combine(repoRoot, "backend", ".env")
        };

        foreach (var envFile in envFiles.Where(File.Exists))
        {
            foreach (var rawLine in File.ReadAllLines(envFile))
            {
                var line = rawLine.Trim();
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
                {
                    continue;
                }

                var separatorIndex = line.IndexOf('=');
                if (separatorIndex <= 0)
                {
                    continue;
                }

                var key = line[..separatorIndex].Trim();
                var value = line[(separatorIndex + 1)..].Trim().Trim('"');
                if (string.IsNullOrWhiteSpace(key) || Environment.GetEnvironmentVariable(key) is not null)
                {
                    continue;
                }

                Environment.SetEnvironmentVariable(key, value);
            }
        }
    }

    private static SeederOptions ParseOptions(string[] args)
    {
        var options = new SeederOptions();

        foreach (var arg in args)
        {
            switch (arg.Trim())
            {
                case "--reset-shows":
                    options.ResetShows = true;
                    break;
                case "--reset-all":
                    options.ResetAll = true;
                    break;
                case "--help":
                case "-h":
                case "/?":
                    options.ShowHelp = true;
                    break;
                case "--force":
                    options.Force = true;
                    break;
                default:
                    throw new ArgumentException($"Argomento non supportato: {arg}");
            }
        }

        if (options.ResetShows && options.ResetAll)
        {
            throw new ArgumentException("Usa solo una modalità di reset alla volta: --reset-shows oppure --reset-all.");
        }

        return options;
    }

    private static void PrintHelp()
    {
        Console.WriteLine("Uso: dotnet run --project backend/scripts/FilmApiSeeder/FilmApiSeeder.csproj [opzioni]");
        Console.WriteLine("Opzioni:");
        Console.WriteLine("  --reset-shows  Elimina programmazione e dati ticketing collegati, poi rigenera gli show.");
        Console.WriteLine("  --reset-all    Elimina anche film, registi, cinema, sale e posti seedati, poi rigenera tutto.");
        Console.WriteLine("  --force        Conferma esplicita richiesta per usare le modalità di reset.");
        Console.WriteLine("  --help         Mostra questo help.");
    }

    private sealed record SeededFilm(Film Film, IReadOnlyList<string> Categorie);
    private sealed record SalaSeed(int NumeroProgressivo, TipoSala TipoSala, string Nome, decimal Supplemento);
    private sealed record SeatSeed(string Settore, int Fila, int Numero, int PosX, int PosY, bool IsWheelchair);
    private sealed record LayoutProfile(int TotalRows, int SideSeats, int CenterSeats, int SideRearSeats, int VipSeats);
    private sealed class SeederOptions
    {
        public bool ResetShows { get; set; }
        public bool ResetAll { get; set; }
        public bool Force { get; set; }
        public bool ShowHelp { get; set; }
    }
}
