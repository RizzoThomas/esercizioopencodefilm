using System.Net;
using System.Net.Http.Json;
using FilmAPI.DTO;
using FilmAPI.Model;

namespace FilmAPI.Tests.Integration;

/// <summary>Suite di test per ProgrammazioneIntegrationTests.</summary>
public class ProgrammazioneIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ProgrammazioneIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    /// <summary>Verifica lo scenario di PG1_GetProgrammazioneFilms_ReturnsEmptyList: predispone i dati e le condizioni previste dal caso di test e controlla che l'esito atteso venga restituito.</summary>
    [Fact]
    public async Task PG1_GetProgrammazioneFilms_ReturnsEmptyList()
    {
        await _factory.ResetDatabaseAsync();
        var client = _factory.CreateAnonymousClient();

        var response = await client.GetAsync("/programmazione/films");

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<ProgrammazioneFilmPagedResultDTO>();
        Assert.NotNull(payload);
        Assert.Empty(payload.Items);
    }

    /// <summary>Verifica lo scenario di PG2_GetProgrammazioneFilms_TabEvidenza_ReturnsFilmsWithShows: predispone i dati e le condizioni previste dal caso di test e controlla che l'esito atteso venga restituito.</summary>
    [Fact]
    public async Task PG2_GetProgrammazioneFilms_TabEvidenza_ReturnsFilmsWithShows()
    {
        await _factory.ResetDatabaseAsync(db => SeedFilmsAndShowsAsync(db));

        var client = _factory.CreateAnonymousClient();
        var response = await client.GetAsync("/programmazione/films?tab=evidenza");

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<ProgrammazioneFilmPagedResultDTO>();
        Assert.NotNull(payload);
        Assert.All(payload.Items, f => Assert.True(f.InEvidenza));
        Assert.All(payload.Items, f => Assert.True(f.ShowCountNext7Days > 0));
    }

    /// <summary>Verifica lo scenario di PG3_GetProgrammazioneFilms_TabUscita_ReturnsUpcomingFilms: predispone i dati e le condizioni previste dal caso di test e controlla che l'esito atteso venga restituito.</summary>
    [Fact]
    public async Task PG3_GetProgrammazioneFilms_TabUscita_ReturnsUpcomingFilms()
    {
        await _factory.ResetDatabaseAsync(db => SeedUpcomingFilmsAsync(db));

        var client = _factory.CreateAnonymousClient();
        var response = await client.GetAsync("/programmazione/films?tab=uscita");

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<ProgrammazioneFilmPagedResultDTO>();
        Assert.NotNull(payload);
        Assert.Single(payload.Items);
        Assert.Equal("Film Futuro", payload.Items[0].Titolo);
        Assert.All(payload.Items, f => Assert.True(f.InUscita));
    }

    /// <summary>Verifica lo scenario di PG3B_GetProgrammazioneFilms_TabUscita_ExcludesFilmsWithShowsToday: predispone i dati e le condizioni previste dal caso di test e controlla che l'esito atteso venga restituito.</summary>
    [Fact]
    public async Task PG3B_GetProgrammazioneFilms_TabUscita_ExcludesFilmsWithShowsToday()
    {
        await _factory.ResetDatabaseAsync(db => SeedUpcomingFilmsWithShowTodayAsync(db));

        var client = _factory.CreateAnonymousClient();
        var response = await client.GetAsync("/programmazione/films?tab=uscita");

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<ProgrammazioneFilmPagedResultDTO>();
        Assert.NotNull(payload);
        Assert.Empty(payload.Items);
    }

    /// <summary>Verifica lo scenario di PG3C_GetProgrammazioneFilms_TabUscita_WithCinemaId_ExcludesFilmsAlreadyAvailableInSelectedCinema: predispone i dati e le condizioni previste dal caso di test e controlla che l'esito atteso venga restituito.</summary>
    [Fact]
    public async Task PG3C_GetProgrammazioneFilms_TabUscita_WithCinemaId_ExcludesFilmsAlreadyAvailableInSelectedCinema()
    {
        await _factory.ResetDatabaseAsync(db => SeedUpcomingFilmsAvailableInSelectedCinemaAsync(db));

        var client = _factory.CreateAnonymousClient();
        var response = await client.GetAsync("/programmazione/films?tab=uscita&cinemaId=1");

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<ProgrammazioneFilmPagedResultDTO>();
        Assert.NotNull(payload);
        Assert.Empty(payload.Items);
    }

    /// <summary>Verifica lo scenario di PG4_GetProgrammazioneFilms_TabTutti_ReturnsAllRelevantFilms: predispone i dati e le condizioni previste dal caso di test e controlla che l'esito atteso venga restituito.</summary>
    [Fact]
    public async Task PG4_GetProgrammazioneFilms_TabTutti_ReturnsAllRelevantFilms()
    {
        await _factory.ResetDatabaseAsync(db => SeedFilmsAndShowsAsync(db));

        var client = _factory.CreateAnonymousClient();
        var response = await client.GetAsync("/programmazione/films?tab=tutti");

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<ProgrammazioneFilmPagedResultDTO>();
        Assert.NotNull(payload);
        Assert.NotEmpty(payload.Items);
    }

    /// <summary>Verifica lo scenario di PG5_GetProgrammazioneFilms_SearchByTitle_ReturnsMatchingFilms: predispone i dati e le condizioni previste dal caso di test e controlla che l'esito atteso venga restituito.</summary>
    [Fact]
    public async Task PG5_GetProgrammazioneFilms_SearchByTitle_ReturnsMatchingFilms()
    {
        await _factory.ResetDatabaseAsync(db => SeedFilmsAndShowsAsync(db));

        var client = _factory.CreateAnonymousClient();
        var response = await client.GetAsync("/programmazione/films?search=Avatar");

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<ProgrammazioneFilmPagedResultDTO>();
        Assert.NotNull(payload);
        Assert.All(payload.Items, f => Assert.Contains("Avatar", f.Titolo, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Verifica lo scenario di PG6_GetProgrammazioneFilms_FilterByCategoria_ReturnsMatchingFilms: predispone i dati e le condizioni previste dal caso di test e controlla che l'esito atteso venga restituito.</summary>
    [Fact]
    public async Task PG6_GetProgrammazioneFilms_FilterByCategoria_ReturnsMatchingFilms()
    {
        await _factory.ResetDatabaseAsync(db => SeedFilmsWithCategoriesAsync(db));

        var client = _factory.CreateAnonymousClient();
        var response = await client.GetAsync($"/programmazione/films?categoriaId={SeedDataCategoriaId}");

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<ProgrammazioneFilmPagedResultDTO>();
        Assert.NotNull(payload);
        Assert.All(payload.Items, f => Assert.Contains(f.Categorie, c => c.Id == SeedDataCategoriaId));
    }

    /// <summary>Verifica lo scenario di PG7_GetProgrammazioneCinemas_WithoutCoords_ReturnsSortedByName: predispone i dati e le condizioni previste dal caso di test e controlla che l'esito atteso venga restituito.</summary>
    [Fact]
    public async Task PG7_GetProgrammazioneCinemas_WithoutCoords_ReturnsSortedByName()
    {
        await _factory.ResetDatabaseAsync(db => SeedCinemasAsync(db));

        var client = _factory.CreateAnonymousClient();
        var response = await client.GetAsync("/programmazione/cinemas");

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<List<CinemaCardDTO>>();
        Assert.NotNull(payload);
        Assert.Equal(3, payload.Count);
        Assert.Equal("Alpha Cinema", payload[0].Nome);
    }

    /// <summary>Verifica lo scenario di PG8_GetProgrammazioneCinemas_WithCoords_ReturnsSortedByDistance: predispone i dati e le condizioni previste dal caso di test e controlla che l'esito atteso venga restituito.</summary>
    [Fact]
    public async Task PG8_GetProgrammazioneCinemas_WithCoords_ReturnsSortedByDistance()
    {
        await _factory.ResetDatabaseAsync(db => SeedCinemasWithCoordsAsync(db));

        var client = _factory.CreateAnonymousClient();
        var response = await client.GetAsync("/programmazione/cinemas?lat=41.9028&lng=12.4964");

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<List<CinemaCardDTO>>();
        Assert.NotNull(payload);
        Assert.Equal(3, payload.Count);
        Assert.NotNull(payload[0].DistanzaKm);
        Assert.True(payload[0].DistanzaKm <= payload[1].DistanzaKm);
        Assert.True(payload[1].DistanzaKm <= payload[2].DistanzaKm);
    }

    /// <summary>Verifica lo scenario di PG9_GetFilmScheda_ReturnsFilmWithShowCalendar: predispone i dati e le condizioni previste dal caso di test e controlla che l'esito atteso venga restituito.</summary>
    [Fact]
    public async Task PG9_GetFilmScheda_ReturnsFilmWithShowCalendar()
    {
        await _factory.ResetDatabaseAsync(db => SeedFilmsAndShowsAsync(db));

        var client = _factory.CreateAnonymousClient();
        var response = await client.GetAsync("/films/1/scheda");

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<FilmSchedaDTO>();
        Assert.NotNull(payload);
        Assert.Equal(1, payload.Id);
        Assert.Equal("Film Uno", payload.Titolo);
        Assert.NotEmpty(payload.ShowCalendar);
    }

    /// <summary>Verifica lo scenario di PG10_GetFilmScheda_WithCinemaId_FiltersShowsByCinema: predispone i dati e le condizioni previste dal caso di test e controlla che l'esito atteso venga restituito.</summary>
    [Fact]
    public async Task PG10_GetFilmScheda_WithCinemaId_FiltersShowsByCinema()
    {
        await _factory.ResetDatabaseAsync(db => SeedFilmsAndShowsAsync(db));

        var client = _factory.CreateAnonymousClient();
        var response = await client.GetAsync("/films/1/scheda?cinemaId=1");

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<FilmSchedaDTO>();
        Assert.NotNull(payload);
        Assert.NotNull(payload.CinemaSelezionato);
        Assert.Equal(1, payload.CinemaSelezionato.Id);
    }

    /// <summary>Verifica lo scenario di PG11_GetFilmScheda_ReturnsNotFound_WhenFilmMissing: predispone i dati e le condizioni previste dal caso di test e controlla che l'esito atteso venga restituito.</summary>
    [Fact]
    public async Task PG11_GetFilmScheda_ReturnsNotFound_WhenFilmMissing()
    {
        await _factory.ResetDatabaseAsync();

        var client = _factory.CreateAnonymousClient();
        var response = await client.GetAsync("/films/99999/scheda");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>Verifica lo scenario di PG12_GetMyCinemas_ReturnsAllCinemas: predispone i dati e le condizioni previste dal caso di test e controlla che l'esito atteso venga restituito.</summary>
    [Fact]
    public async Task PG12_GetMyCinemas_ReturnsAllCinemas()
    {
        await _factory.ResetDatabaseAsync(db => SeedCinemasWithSaleAsync(db));

        var client = _factory.CreateAnonymousClient();
        var response = await client.GetAsync("/my-cinemas");

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<List<CinemaCardDTO>>();
        Assert.NotNull(payload);
        Assert.NotEmpty(payload);
        Assert.NotEmpty(payload[0].TipologieSalePresenti);
    }

    /// <summary>Verifica lo scenario di PG13_GetCinemaSchedule_ReturnsScheduleForDate: predispone i dati e le condizioni previste dal caso di test e controlla che l'esito atteso venga restituito.</summary>
    [Fact]
    public async Task PG13_GetCinemaSchedule_ReturnsScheduleForDate()
    {
        await _factory.ResetDatabaseAsync(db => SeedFilmsAndShowsAsync(db));

        var client = _factory.CreateAnonymousClient();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var response = await client.GetAsync($"/my-cinemas/1/schedule?date={today:yyyy-MM-dd}");

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<CinemaScheduleDayDTO>();
        Assert.NotNull(payload);
        Assert.Equal(1, payload.Cinema.Id);
        Assert.Equal(today, payload.Data);
    }

    /// <summary>Verifica lo scenario di PG14_GetCinemaSchedule_ReturnsNotFound_WhenCinemaMissing: predispone i dati e le condizioni previste dal caso di test e controlla che l'esito atteso venga restituito.</summary>
    [Fact]
    public async Task PG14_GetCinemaSchedule_ReturnsNotFound_WhenCinemaMissing()
    {
        await _factory.ResetDatabaseAsync();

        var client = _factory.CreateAnonymousClient();
        var response = await client.GetAsync("/my-cinemas/99999/schedule");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>Verifica lo scenario di PG15_GetCinemaPreferito_ReturnsNull_WhenNotSet: predispone i dati e le condizioni previste dal caso di test e controlla che l'esito atteso venga restituito.</summary>
    [Fact]
    public async Task PG15_GetCinemaPreferito_ReturnsNull_WhenNotSet()
    {
        await _factory.ResetDatabaseAsync(db => SeedUserAsync(db));

        var client = _factory.CreateUserClient(1);
        var response = await client.GetAsync("/profilo/cinema-preferito");

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<CinemaPreferitoDTO>();
        Assert.NotNull(payload);
        Assert.Null(payload.CinemaId);
        Assert.Null(payload.Cinema);
    }

    /// <summary>Verifica lo scenario di PG16_PutCinemaPreferito_SetsCinema: predispone i dati e le condizioni previste dal caso di test e controlla che l'esito atteso venga restituito.</summary>
    [Fact]
    public async Task PG16_PutCinemaPreferito_SetsCinema()
    {
        await _factory.ResetDatabaseAsync(db => SeedUserAndCinemaAsync(db));

        var client = _factory.CreateUserClient(1);
        var response = await client.PutAsJsonAsync("/profilo/cinema-preferito/1", new { });

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<CinemaPreferitoDTO>();
        Assert.NotNull(payload);
        Assert.Equal(1, payload.CinemaId);
        Assert.NotNull(payload.Cinema);
        Assert.Equal("Cinema Test", payload.Cinema.Nome);
    }

    /// <summary>Verifica lo scenario di PG17_PutCinemaPreferito_ClearsCinema: predispone i dati e le condizioni previste dal caso di test e controlla che l'esito atteso venga restituito.</summary>
    [Fact]
    public async Task PG17_PutCinemaPreferito_ClearsCinema()
    {
        await _factory.ResetDatabaseAsync(db => SeedUserAndCinemaAsync(db, setPreferred: true));

        var client = _factory.CreateUserClient(1);
        var response = await client.PutAsJsonAsync("/profilo/cinema-preferito", new { });

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<CinemaPreferitoDTO>();
        Assert.NotNull(payload);
        Assert.Null(payload.CinemaId);
    }

    /// <summary>Verifica lo scenario di PG18_PutCinemaPreferito_ReturnsBadRequest_WhenCinemaNotFound: predispone i dati e le condizioni previste dal caso di test e controlla che l'esito atteso venga restituito.</summary>
    [Fact]
    public async Task PG18_PutCinemaPreferito_ReturnsBadRequest_WhenCinemaNotFound()
    {
        await _factory.ResetDatabaseAsync(db => SeedUserAsync(db));

        var client = _factory.CreateUserClient(1);
        var response = await client.PutAsJsonAsync("/profilo/cinema-preferito/99999", new { });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>Verifica lo scenario di PG19_GetProgrammazioneFilms_WithCinemaId_ShowsAvailability: predispone i dati e le condizioni previste dal caso di test e controlla che l'esito atteso venga restituito.</summary>
    [Fact]
    public async Task PG19_GetProgrammazioneFilms_WithCinemaId_ShowsAvailability()
    {
        await _factory.ResetDatabaseAsync(db => SeedFilmsAndShowsAsync(db));

        var client = _factory.CreateAnonymousClient();
        var response = await client.GetAsync("/programmazione/films?cinemaId=1");

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<ProgrammazioneFilmPagedResultDTO>();
        Assert.NotNull(payload);
        var filmUno = payload.Items.FirstOrDefault(f => f.Id == 1);
        Assert.NotNull(filmUno);
        Assert.True(filmUno.DisponibileNelCinemaSelezionato);
    }

    /// <summary>Verifica lo scenario di PG20_GetProgrammazioneFilms_OrderByShowCount_Evidenza: predispone i dati e le condizioni previste dal caso di test e controlla che l'esito atteso venga restituito.</summary>
    [Fact]
    public async Task PG20_GetProgrammazioneFilms_OrderByShowCount_Evidenza()
    {
        await _factory.ResetDatabaseAsync(db => SeedFilmsWithDifferentShowCountsAsync(db));

        var client = _factory.CreateAnonymousClient();
        var response = await client.GetAsync("/programmazione/films?tab=evidenza");

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<ProgrammazioneFilmPagedResultDTO>();
        Assert.NotNull(payload);
        Assert.True(payload.Items[0].ShowCountNext7Days >= payload.Items[payload.Items.Count - 1].ShowCountNext7Days);
    }

    /// <summary>Verifica lo scenario di PG21_GetProgrammazioneFilms_WithPagination_ReturnsPagedResult: predispone i dati e le condizioni previste dal caso di test e controlla che l'esito atteso venga restituito.</summary>
    [Fact]
    public async Task PG21_GetProgrammazioneFilms_WithPagination_ReturnsPagedResult()
    {
        await _factory.ResetDatabaseAsync(db => SeedManyRelevantFilmsAsync(db, 25));

        var client = _factory.CreateAnonymousClient();
        var response = await client.GetAsync("/programmazione/films?tab=tutti&page=2&pageSize=10");

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<ProgrammazioneFilmPagedResultDTO>();
        Assert.NotNull(payload);
        Assert.Equal(2, payload.Page);
        Assert.Equal(10, payload.PageSize);
        Assert.Equal(25, payload.TotalCount);
        Assert.Equal(3, payload.TotalPages);
        Assert.True(payload.HasNextPage);
        Assert.True(payload.HasPreviousPage);
        Assert.Equal(10, payload.Items.Count);
    }

    private int SeedDataCategoriaId { get; set; }

    private async Task SeedFilmsAndShowsAsync(FilmAPI.Data.FilmDbContext db)
    {
        var regista = new Regista { Nome = "Regista", Cognome = "Test", Nazionalita = "IT" };
        db.Registi.Add(regista);
        await db.SaveChangesAsync();

        var cinema1 = new Cinema { Nome = "Cinema Uno", Indirizzo = "Via Roma 1", Citta = "Roma" };
        var cinema2 = new Cinema { Nome = "Cinema Due", Indirizzo = "Via Milano 2", Citta = "Milano" };
        db.Cinemas.AddRange(cinema1, cinema2);
        await db.SaveChangesAsync();

        var sala1 = new Sala { CinemaId = cinema1.Id, NumeroProgressivo = 1, TipoSala = TipoSala.DueD, Nome = "Sala 1", Supplemento = 0, IsAttiva = true };
        var sala2 = new Sala { CinemaId = cinema2.Id, NumeroProgressivo = 1, TipoSala = TipoSala.DueD, Nome = "Sala 1", Supplemento = 0, IsAttiva = true };
        db.Sale.AddRange(sala1, sala2);
        await db.SaveChangesAsync();

        var film1 = new Film
        {
            Titolo = "Film Uno",
            DataProduzione = new DateTime(2024, 1, 1),
            RegistaId = regista.Id,
            Durata = 120,
            DescrizioneLunga = "Descrizione lunga del film uno",
            CastText = "Attore Uno, Attore Due, Attore Tre",
            DataRilascio = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-5))
        };
        var film2 = new Film
        {
            Titolo = "Film Due",
            DataProduzione = new DateTime(2024, 6, 1),
            RegistaId = regista.Id,
            Durata = 90,
            DescrizioneLunga = "Descrizione del film due",
            CastText = "Attore Quattro",
            DataRilascio = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-10))
        };
        db.Films.AddRange(film1, film2);
        await db.SaveChangesAsync();

        var now = DateTime.UtcNow;
        var shows = new List<Show>
        {
            new Show { CinemaId = cinema1.Id, SalaId = sala1.Id, FilmId = film1.Id, StartAtUtc = now.AddHours(2), DurataMinutiSnapshot = 120, PrezzoBase = 10, SupplementoSala = 0 },
            new Show { CinemaId = cinema1.Id, SalaId = sala1.Id, FilmId = film1.Id, StartAtUtc = now.AddHours(5), DurataMinutiSnapshot = 120, PrezzoBase = 10, SupplementoSala = 0 },
            new Show { CinemaId = cinema1.Id, SalaId = sala1.Id, FilmId = film2.Id, StartAtUtc = now.AddHours(8), DurataMinutiSnapshot = 90, PrezzoBase = 10, SupplementoSala = 0 },
            new Show { CinemaId = cinema2.Id, SalaId = sala2.Id, FilmId = film1.Id, StartAtUtc = now.AddHours(3), DurataMinutiSnapshot = 120, PrezzoBase = 10, SupplementoSala = 0 },
        };
        db.Shows.AddRange(shows);
        await db.SaveChangesAsync();
    }

    private async Task SeedUpcomingFilmsAsync(FilmAPI.Data.FilmDbContext db)
    {
        var regista = new Regista { Nome = "Regista", Cognome = "Test", Nazionalita = "IT" };
        db.Registi.Add(regista);
        await db.SaveChangesAsync();

        var upcomingFilm = new Film
        {
            Titolo = "Film Futuro",
            DataProduzione = new DateTime(2025, 1, 1),
            RegistaId = regista.Id,
            Durata = 120,
            DataRilascio = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7))
        };
        var oldFilm = new Film
        {
            Titolo = "Film Vecchio",
            DataProduzione = new DateTime(2020, 1, 1),
            RegistaId = regista.Id,
            Durata = 90,
            DataRilascio = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30))
        };
        db.Films.AddRange(upcomingFilm, oldFilm);
        await db.SaveChangesAsync();
    }

    private async Task SeedUpcomingFilmsWithShowTodayAsync(FilmAPI.Data.FilmDbContext db)
    {
        var regista = new Regista { Nome = "Regista", Cognome = "Test", Nazionalita = "IT" };
        db.Registi.Add(regista);
        await db.SaveChangesAsync();

        var cinema = new Cinema { Nome = "Cinema Test", Indirizzo = "Via Test 1", Citta = "Roma" };
        db.Cinemas.Add(cinema);
        await db.SaveChangesAsync();

        var sala = new Sala { CinemaId = cinema.Id, NumeroProgressivo = 1, TipoSala = TipoSala.DueD, Nome = "Sala 1", Supplemento = 0, IsAttiva = true };
        db.Sale.Add(sala);
        await db.SaveChangesAsync();

        var upcomingFilm = new Film
        {
            Titolo = "Film Futuro Con Show Oggi",
            DataProduzione = new DateTime(2025, 1, 1),
            RegistaId = regista.Id,
            Durata = 120,
            DataRilascio = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7))
        };
        db.Films.Add(upcomingFilm);
        await db.SaveChangesAsync();

        db.Shows.Add(new Show
        {
            CinemaId = cinema.Id,
            SalaId = sala.Id,
            FilmId = upcomingFilm.Id,
            StartAtUtc = DateTime.UtcNow.Date.AddHours(20),
            DurataMinutiSnapshot = 120,
            PrezzoBase = 10,
            SupplementoSala = 0
        });
        await db.SaveChangesAsync();
    }

    private async Task SeedUpcomingFilmsAvailableInSelectedCinemaAsync(FilmAPI.Data.FilmDbContext db)
    {
        var regista = new Regista { Nome = "Regista", Cognome = "Test", Nazionalita = "IT" };
        db.Registi.Add(regista);
        await db.SaveChangesAsync();

        var cinema = new Cinema { Nome = "Cinema Selezionato", Indirizzo = "Via Test 1", Citta = "Roma" };
        db.Cinemas.Add(cinema);
        await db.SaveChangesAsync();

        var sala = new Sala { CinemaId = cinema.Id, NumeroProgressivo = 1, TipoSala = TipoSala.DueD, Nome = "Sala 1", Supplemento = 0, IsAttiva = true };
        db.Sale.Add(sala);
        await db.SaveChangesAsync();

        var upcomingFilm = new Film
        {
            Titolo = "Film Futuro Gia Disponibile",
            DataProduzione = new DateTime(2025, 1, 1),
            RegistaId = regista.Id,
            Durata = 120,
            DataRilascio = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7))
        };
        db.Films.Add(upcomingFilm);
        await db.SaveChangesAsync();

        db.Shows.Add(new Show
        {
            CinemaId = cinema.Id,
            SalaId = sala.Id,
            FilmId = upcomingFilm.Id,
            StartAtUtc = DateTime.UtcNow.AddDays(2),
            DurataMinutiSnapshot = 120,
            PrezzoBase = 10,
            SupplementoSala = 0
        });
        await db.SaveChangesAsync();
    }

    private async Task SeedFilmsWithCategoriesAsync(FilmAPI.Data.FilmDbContext db)
    {
        var regista = new Regista { Nome = "Regista", Cognome = "Test", Nazionalita = "IT" };
        db.Registi.Add(regista);
        await db.SaveChangesAsync();

        var categoria = new Categoria { Nome = "Azione" };
        db.Categorie.Add(categoria);
        await db.SaveChangesAsync();
        SeedDataCategoriaId = categoria.Id;

        var cinema = new Cinema { Nome = "Cinema", Indirizzo = "Via 1", Citta = "Roma" };
        db.Cinemas.Add(cinema);
        await db.SaveChangesAsync();

        var sala = new Sala { CinemaId = cinema.Id, NumeroProgressivo = 1, TipoSala = TipoSala.DueD, Nome = "Sala 1", Supplemento = 0, IsAttiva = true };
        db.Sale.Add(sala);
        await db.SaveChangesAsync();

        var film1 = new Film
        {
            Titolo = "Film Azione",
            DataProduzione = new DateTime(2024, 1, 1),
            RegistaId = regista.Id,
            Durata = 120
        };
        var film2 = new Film
        {
            Titolo = "Film Altro",
            DataProduzione = new DateTime(2024, 2, 1),
            RegistaId = regista.Id,
            Durata = 90
        };
        db.Films.AddRange(film1, film2);
        await db.SaveChangesAsync();

        db.FilmCategorie.Add(new FilmCategoria { FilmId = film1.Id, CategoriaId = categoria.Id });
        await db.SaveChangesAsync();

        var now = DateTime.UtcNow;
        db.Shows.AddRange(
            new Show { CinemaId = cinema.Id, SalaId = sala.Id, FilmId = film1.Id, StartAtUtc = now.AddHours(2), DurataMinutiSnapshot = 120, PrezzoBase = 10, SupplementoSala = 0 },
            new Show { CinemaId = cinema.Id, SalaId = sala.Id, FilmId = film2.Id, StartAtUtc = now.AddHours(5), DurataMinutiSnapshot = 90, PrezzoBase = 10, SupplementoSala = 0 }
        );
        await db.SaveChangesAsync();
    }

    private async Task SeedCinemasAsync(FilmAPI.Data.FilmDbContext db)
    {
        db.Cinemas.AddRange(
            new Cinema { Nome = "Alpha Cinema", Indirizzo = "Via A 1", Citta = "Roma" },
            new Cinema { Nome = "Beta Cinema", Indirizzo = "Via B 2", Citta = "Milano" },
            new Cinema { Nome = "Gamma Cinema", Indirizzo = "Via C 3", Citta = "Napoli" }
        );
        await db.SaveChangesAsync();
    }

    private async Task SeedCinemasWithCoordsAsync(FilmAPI.Data.FilmDbContext db)
    {
        db.Cinemas.AddRange(
            new Cinema { Nome = "Cinema Lontano", Indirizzo = "Via 1", Citta = "Napoli", Latitudine = 40.8518, Longitudine = 14.2681 },
            new Cinema { Nome = "Cinema Vicino", Indirizzo = "Via 2", Citta = "Roma", Latitudine = 41.9029, Longitudine = 12.4965 },
            new Cinema { Nome = "Cinema Medio", Indirizzo = "Via 3", Citta = "Firenze", Latitudine = 43.7696, Longitudine = 11.2558 }
        );
        await db.SaveChangesAsync();
    }

    private async Task SeedCinemasWithSaleAsync(FilmAPI.Data.FilmDbContext db)
    {
        var cinema = new Cinema { Nome = "Cinema Multi", Indirizzo = "Via 1", Citta = "Roma" };
        db.Cinemas.Add(cinema);
        await db.SaveChangesAsync();

        db.Sale.AddRange(
            new Sala { CinemaId = cinema.Id, NumeroProgressivo = 1, TipoSala = TipoSala.DueD, Nome = "Sala 1", Supplemento = 0, IsAttiva = true },
            new Sala { CinemaId = cinema.Id, NumeroProgressivo = 2, TipoSala = TipoSala.TreD, Nome = "Sala 2", Supplemento = 2, IsAttiva = true },
            new Sala { CinemaId = cinema.Id, NumeroProgressivo = 3, TipoSala = TipoSala.ISENSE, Nome = "Sala 3", Supplemento = 4, IsAttiva = true }
        );
        await db.SaveChangesAsync();
    }

    private async Task SeedUserAsync(FilmAPI.Data.FilmDbContext db)
    {
        var user = new User
        {
            Email = "test@test.com",
            PasswordHash = "hash",
            Nome = "Test",
            Cognome = "User",
            Ruolo = UserRole.User,
            DataRegistrazione = DateTime.UtcNow
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
    }

    private async Task SeedUserAndCinemaAsync(FilmAPI.Data.FilmDbContext db, bool setPreferred = false)
    {
        var cinema = new Cinema { Nome = "Cinema Test", Indirizzo = "Via Test 1", Citta = "Roma" };
        db.Cinemas.Add(cinema);
        await db.SaveChangesAsync();

        var user = new User
        {
            Email = "test@test.com",
            PasswordHash = "hash",
            Nome = "Test",
            Cognome = "User",
            Ruolo = UserRole.User,
            DataRegistrazione = DateTime.UtcNow,
            CinemaPreferitoId = setPreferred ? cinema.Id : null
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
    }

    private async Task SeedFilmsWithDifferentShowCountsAsync(FilmAPI.Data.FilmDbContext db)
    {
        var regista = new Regista { Nome = "Regista", Cognome = "Test", Nazionalita = "IT" };
        db.Registi.Add(regista);
        await db.SaveChangesAsync();

        var cinema = new Cinema { Nome = "Cinema", Indirizzo = "Via 1", Citta = "Roma" };
        db.Cinemas.Add(cinema);
        await db.SaveChangesAsync();

        var sala = new Sala { CinemaId = cinema.Id, NumeroProgressivo = 1, TipoSala = TipoSala.DueD, Nome = "Sala 1", Supplemento = 0, IsAttiva = true };
        db.Sale.Add(sala);
        await db.SaveChangesAsync();

        var film1 = new Film { Titolo = "Film Popolare", DataProduzione = new DateTime(2024, 1, 1), RegistaId = regista.Id, Durata = 120 };
        var film2 = new Film { Titolo = "Film Poco Show", DataProduzione = new DateTime(2024, 2, 1), RegistaId = regista.Id, Durata = 90 };
        var film3 = new Film { Titolo = "Film Niente Show", DataProduzione = new DateTime(2024, 3, 1), RegistaId = regista.Id, Durata = 100 };
        db.Films.AddRange(film1, film2, film3);
        await db.SaveChangesAsync();

        var now = DateTime.UtcNow;
        var shows = new List<Show>
        {
            new Show { CinemaId = cinema.Id, SalaId = sala.Id, FilmId = film1.Id, StartAtUtc = now.AddHours(1), DurataMinutiSnapshot = 120, PrezzoBase = 10, SupplementoSala = 0 },
            new Show { CinemaId = cinema.Id, SalaId = sala.Id, FilmId = film1.Id, StartAtUtc = now.AddHours(3), DurataMinutiSnapshot = 120, PrezzoBase = 10, SupplementoSala = 0 },
            new Show { CinemaId = cinema.Id, SalaId = sala.Id, FilmId = film1.Id, StartAtUtc = now.AddHours(5), DurataMinutiSnapshot = 120, PrezzoBase = 10, SupplementoSala = 0 },
            new Show { CinemaId = cinema.Id, SalaId = sala.Id, FilmId = film1.Id, StartAtUtc = now.AddHours(7), DurataMinutiSnapshot = 120, PrezzoBase = 10, SupplementoSala = 0 },
            new Show { CinemaId = cinema.Id, SalaId = sala.Id, FilmId = film1.Id, StartAtUtc = now.AddHours(9), DurataMinutiSnapshot = 120, PrezzoBase = 10, SupplementoSala = 0 },
            new Show { CinemaId = cinema.Id, SalaId = sala.Id, FilmId = film2.Id, StartAtUtc = now.AddHours(2), DurataMinutiSnapshot = 90, PrezzoBase = 10, SupplementoSala = 0 },
        };
        db.Shows.AddRange(shows);
        await db.SaveChangesAsync();
    }

    private async Task SeedManyRelevantFilmsAsync(FilmAPI.Data.FilmDbContext db, int count)
    {
        var regista = new Regista { Nome = "Regista", Cognome = "Bulk", Nazionalita = "IT" };
        db.Registi.Add(regista);
        await db.SaveChangesAsync();

        for (var i = 1; i <= count; i++)
        {
            db.Films.Add(new Film
            {
                Titolo = $"Film Bulk {i:D2}",
                DataProduzione = new DateTime(2024, 1, 1),
                RegistaId = regista.Id,
                Durata = 100,
                DataRilascio = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-i))
            });
        }

        await db.SaveChangesAsync();
    }
}
