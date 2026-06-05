using System.Net;
using System.Net.Http.Json;
using FilmAPI.DTO;
using FilmAPI.Model;
using Microsoft.EntityFrameworkCore;

namespace FilmAPI.Tests.Integration;

/// <summary>Suite di test per ShowIntegrationTests.</summary>
public class ShowIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ShowIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    /// <summary>Verifica lo scenario di SH1_GetShows_ReturnsEmptyList: predispone i dati e le condizioni previste dal caso di test e controlla che l'esito atteso venga restituito.</summary>
    [Fact]
    public async Task SH1_GetShows_ReturnsEmptyList()
    {
        await _factory.ResetDatabaseAsync();
        var client = _factory.CreateAnonymousClient();

        var response = await client.GetAsync("/shows");

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<List<ShowDTO>>();
        Assert.NotNull(payload);
        Assert.Empty(payload);
    }

    /// <summary>Verifica lo scenario di SH2_GetShows_ReturnsAllShows: predispone i dati e le condizioni previste dal caso di test e controlla che l'esito atteso venga restituito.</summary>
    [Fact]
    public async Task SH2_GetShows_ReturnsAllShows()
    {
        await _factory.ResetDatabaseAsync(db => SeedCinemaSalaFilmAndShowAsync(db));
        var client = _factory.CreateAnonymousClient();

        var response = await client.GetAsync("/shows");

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<List<ShowDTO>>();
        Assert.NotNull(payload);
        Assert.Single(payload);
        Assert.Equal("Film Test", payload[0].FilmTitolo);
        Assert.Equal("Cinema Test", payload[0].CinemaNome);
        Assert.Equal("Sala 1", payload[0].SalaNome);
    }

    /// <summary>Verifica lo scenario di SH3_GetShowsPaged_ReturnsPagedResult: predispone i dati e le condizioni previste dal caso di test e controlla che l'esito atteso venga restituito.</summary>
    [Fact]
    public async Task SH3_GetShowsPaged_ReturnsPagedResult()
    {
        await _factory.ResetDatabaseAsync(db => SeedCinemaSalaFilmAndShowAsync(db));
        var client = _factory.CreateAnonymousClient();

        var response = await client.GetAsync("/shows?page=1&pageSize=10");

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<ShowPagedResultDTO>();
        Assert.NotNull(payload);
        Assert.Equal(1, payload.TotalCount);
        Assert.Equal(1, payload.Page);
        Assert.Single(payload.Items);
    }

    /// <summary>Verifica lo scenario di SH4_GetShowsByCinema_FiltersByCinemaId: predispone i dati e le condizioni previste dal caso di test e controlla che l'esito atteso venga restituito.</summary>
    [Fact]
    public async Task SH4_GetShowsByCinema_FiltersByCinemaId()
    {
        await _factory.ResetDatabaseAsync(db => SeedCinemaSalaFilmAndShowAsync(db));
        var client = _factory.CreateAnonymousClient();

        var response = await client.GetAsync("/shows?cinemaId=1");

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<ShowPagedResultDTO>();
        Assert.NotNull(payload);
        Assert.Single(payload.Items);
        Assert.All(payload.Items, s => Assert.Equal(1, s.CinemaId));
    }

    /// <summary>Verifica lo scenario di SH5_GetShowsByFilm_FiltersByFilmId: predispone i dati e le condizioni previste dal caso di test e controlla che l'esito atteso venga restituito.</summary>
    [Fact]
    public async Task SH5_GetShowsByFilm_FiltersByFilmId()
    {
        await _factory.ResetDatabaseAsync(db => SeedCinemaSalaFilmAndShowAsync(db));
        var client = _factory.CreateAnonymousClient();

        var response = await client.GetAsync("/shows?filmId=1");

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<ShowPagedResultDTO>();
        Assert.NotNull(payload);
        Assert.Single(payload.Items);
        Assert.All(payload.Items, s => Assert.Equal(1, s.FilmId));
    }

    /// <summary>Verifica lo scenario di SH6_GetShowById_ReturnsShow: predispone i dati e le condizioni previste dal caso di test e controlla che l'esito atteso venga restituito.</summary>
    [Fact]
    public async Task SH6_GetShowById_ReturnsShow()
    {
        await _factory.ResetDatabaseAsync(db => SeedCinemaSalaFilmAndShowAsync(db));
        var client = _factory.CreateAnonymousClient();

        var response = await client.GetAsync("/shows/1");

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<ShowDTO>();
        Assert.NotNull(payload);
        Assert.Equal(1, payload.Id);
        Assert.Equal(1, payload.CinemaId);
        Assert.Equal(1, payload.SalaId);
        Assert.Equal(1, payload.FilmId);
    }

    /// <summary>Verifica lo scenario di SH7_GetShowById_NotFound: predispone i dati e le condizioni previste dal caso di test e controlla che l'esito atteso venga restituito.</summary>
    [Fact]
    public async Task SH7_GetShowById_NotFound()
    {
        await _factory.ResetDatabaseAsync();
        var client = _factory.CreateAnonymousClient();

        var response = await client.GetAsync("/shows/999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>Verifica lo scenario di SH8_CreateShow_ReturnsCreated: predispone i dati e le condizioni previste dal caso di test e controlla che l'esito atteso venga restituito.</summary>
    [Fact]
    public async Task SH8_CreateShow_ReturnsCreated()
    {
        await _factory.ResetDatabaseAsync(db => SeedCinemaSalaFilmAsync(db));
        var client = _factory.CreateAdminClient();

        var dto = new ShowCreateDTO
        {
            CinemaId = 1,
            SalaId = 1,
            FilmId = 1,
            StartAtUtc = new DateTime(2026, 5, 1, 20, 0, 0, DateTimeKind.Utc),
            PrezzoBase = 12.50m
        };

        var response = await client.PostAsJsonAsync("/shows", dto);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ShowDTO>();
        Assert.NotNull(payload);
        Assert.Equal(1, payload.CinemaId);
        Assert.Equal(1, payload.SalaId);
        Assert.Equal(1, payload.FilmId);
        Assert.Equal(12.50m, payload.PrezzoBase);
        Assert.Equal("Film Test", payload.FilmTitolo);
    }

    /// <summary>Verifica lo scenario di SH8B_CreateShow_NormalizesCentBasedPrezzoBase: predispone i dati e le condizioni previste dal caso di test e controlla che l'esito atteso venga restituito.</summary>
    [Fact]
    public async Task SH8B_CreateShow_NormalizesCentBasedPrezzoBase()
    {
        await _factory.ResetDatabaseAsync(db => SeedCinemaSalaFilmAsync(db));
        var client = _factory.CreateAdminClient();

        var dto = new ShowCreateDTO
        {
            CinemaId = 1,
            SalaId = 1,
            FilmId = 1,
            StartAtUtc = new DateTime(2026, 5, 2, 20, 0, 0, DateTimeKind.Utc),
            PrezzoBase = 850m
        };

        var response = await client.PostAsJsonAsync("/shows", dto);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ShowDTO>();
        Assert.NotNull(payload);
        Assert.Equal(8.50m, payload.PrezzoBase);
    }

    /// <summary>Verifica lo scenario di SH9_CreateShow_UsesFilmDurataWhenNotSpecified: predispone i dati e le condizioni previste dal caso di test e controlla che l'esito atteso venga restituito.</summary>
    [Fact]
    public async Task SH9_CreateShow_UsesFilmDurataWhenNotSpecified()
    {
        await _factory.ResetDatabaseAsync(db => SeedCinemaSalaFilmAsync(db));
        var client = _factory.CreateAdminClient();

        var dto = new ShowCreateDTO
        {
            CinemaId = 1,
            SalaId = 1,
            FilmId = 1,
            StartAtUtc = new DateTime(2026, 5, 1, 20, 0, 0, DateTimeKind.Utc)
        };

        var response = await client.PostAsJsonAsync("/shows", dto);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ShowDTO>();
        Assert.NotNull(payload);
        Assert.Equal(120, payload.DurataMinutiSnapshot);
    }

    /// <summary>Verifica lo scenario di SH10_CreateShow_BadRequestOnInvalidCinema: predispone i dati e le condizioni previste dal caso di test e controlla che l'esito atteso venga restituito.</summary>
    [Fact]
    public async Task SH10_CreateShow_BadRequestOnInvalidCinema()
    {
        await _factory.ResetDatabaseAsync(db => SeedCinemaSalaFilmAsync(db));
        var client = _factory.CreateAdminClient();

        var dto = new ShowCreateDTO
        {
            CinemaId = 999,
            SalaId = 1,
            FilmId = 1,
            StartAtUtc = new DateTime(2026, 5, 1, 20, 0, 0, DateTimeKind.Utc)
        };

        var response = await client.PostAsJsonAsync("/shows", dto);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>Verifica lo scenario di SH11_CreateShow_BadRequestOnInvalidSala: predispone i dati e le condizioni previste dal caso di test e controlla che l'esito atteso venga restituito.</summary>
    [Fact]
    public async Task SH11_CreateShow_BadRequestOnInvalidSala()
    {
        await _factory.ResetDatabaseAsync(db => SeedCinemaSalaFilmAsync(db));
        var client = _factory.CreateAdminClient();

        var dto = new ShowCreateDTO
        {
            CinemaId = 1,
            SalaId = 999,
            FilmId = 1,
            StartAtUtc = new DateTime(2026, 5, 1, 20, 0, 0, DateTimeKind.Utc)
        };

        var response = await client.PostAsJsonAsync("/shows", dto);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>Verifica lo scenario di SH12_CreateShow_BadRequestOnInvalidFilm: predispone i dati e le condizioni previste dal caso di test e controlla che l'esito atteso venga restituito.</summary>
    [Fact]
    public async Task SH12_CreateShow_BadRequestOnInvalidFilm()
    {
        await _factory.ResetDatabaseAsync(db => SeedCinemaSalaFilmAsync(db));
        var client = _factory.CreateAdminClient();

        var dto = new ShowCreateDTO
        {
            CinemaId = 1,
            SalaId = 1,
            FilmId = 999,
            StartAtUtc = new DateTime(2026, 5, 1, 20, 0, 0, DateTimeKind.Utc)
        };

        var response = await client.PostAsJsonAsync("/shows", dto);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>Verifica lo scenario di SH13_CreateShow_BadRequestOnSalaNotInCinema: predispone i dati e le condizioni previste dal caso di test e controlla che l'esito atteso venga restituito.</summary>
    [Fact]
    public async Task SH13_CreateShow_BadRequestOnSalaNotInCinema()
    {
        await _factory.ResetDatabaseAsync(db => SeedTwoCinemasWithSaleAsync(db));
        var client = _factory.CreateAdminClient();

        var dto = new ShowCreateDTO
        {
            CinemaId = 1,
            SalaId = 2,
            FilmId = 1,
            StartAtUtc = new DateTime(2026, 5, 1, 20, 0, 0, DateTimeKind.Utc)
        };

        var response = await client.PostAsJsonAsync("/shows", dto);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>Verifica lo scenario di SH14_CreateShow_ConflictOnOverlap: predispone i dati e le condizioni previste dal caso di test e controlla che l'esito atteso venga restituito.</summary>
    [Fact]
    public async Task SH14_CreateShow_ConflictOnOverlap()
    {
        await _factory.ResetDatabaseAsync(db => SeedCinemaSalaFilmAndShowAsync(db));
        var client = _factory.CreateAdminClient();

        var dto = new ShowCreateDTO
        {
            CinemaId = 1,
            SalaId = 1,
            FilmId = 1,
            StartAtUtc = new DateTime(2026, 5, 1, 20, 30, 0, DateTimeKind.Utc)
        };

        var response = await client.PostAsJsonAsync("/shows", dto);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    /// <summary>Verifica lo scenario di SH15_CreateShow_AllowedInDifferentSala: predispone i dati e le condizioni previste dal caso di test e controlla che l'esito atteso venga restituito.</summary>
    [Fact]
    public async Task SH15_CreateShow_AllowedInDifferentSala()
    {
        await _factory.ResetDatabaseAsync(db => SeedCinemaWithTwoSaleAndShowAsync(db));
        var client = _factory.CreateAdminClient();

        var dto = new ShowCreateDTO
        {
            CinemaId = 1,
            SalaId = 2,
            FilmId = 1,
            StartAtUtc = new DateTime(2026, 5, 1, 20, 0, 0, DateTimeKind.Utc)
        };

        var response = await client.PostAsJsonAsync("/shows", dto);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    /// <summary>Verifica lo scenario di SH16_CreateShow_ForbiddenForUser: predispone i dati e le condizioni previste dal caso di test e controlla che l'esito atteso venga restituito.</summary>
    [Fact]
    public async Task SH16_CreateShow_ForbiddenForUser()
    {
        await _factory.ResetDatabaseAsync(db => SeedCinemaSalaFilmAsync(db));
        var client = _factory.CreateUserClient();

        var dto = new ShowCreateDTO
        {
            CinemaId = 1,
            SalaId = 1,
            FilmId = 1,
            StartAtUtc = new DateTime(2026, 5, 1, 20, 0, 0, DateTimeKind.Utc)
        };

        var response = await client.PostAsJsonAsync("/shows", dto);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    /// <summary>Verifica lo scenario di SH17_UpdateShow_UpdatesFields: predispone i dati e le condizioni previste dal caso di test e controlla che l'esito atteso venga restituito.</summary>
    [Fact]
    public async Task SH17_UpdateShow_UpdatesFields()
    {
        await _factory.ResetDatabaseAsync(db => SeedCinemaSalaFilmAndShowAsync(db));
        var client = _factory.CreateAdminClient();

        var dto = new ShowUpdateDTO
        {
            PrezzoBase = 15.00m,
            DurataMinutiSnapshot = 130
        };

        var response = await client.PutAsJsonAsync("/shows/1", dto);

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<ShowDTO>();
        Assert.NotNull(payload);
        Assert.Equal(15.00m, payload.PrezzoBase);
        Assert.Equal(130, payload.DurataMinutiSnapshot);
    }

    /// <summary>Verifica lo scenario di SH18_UpdateShow_NotFound: predispone i dati e le condizioni previste dal caso di test e controlla che l'esito atteso venga restituito.</summary>
    [Fact]
    public async Task SH18_UpdateShow_NotFound()
    {
        await _factory.ResetDatabaseAsync();
        var client = _factory.CreateAdminClient();

        var dto = new ShowUpdateDTO { PrezzoBase = 10m };

        var response = await client.PutAsJsonAsync("/shows/999", dto);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>Verifica lo scenario di SH19_UpdateShow_ConflictOnOverlap: predispone i dati e le condizioni previste dal caso di test e controlla che l'esito atteso venga restituito.</summary>
    [Fact]
    public async Task SH19_UpdateShow_ConflictOnOverlap()
    {
        await _factory.ResetDatabaseAsync(db => SeedCinemaSalaFilmAndTwoShowsAsync(db));
        var client = _factory.CreateAdminClient();

        var dto = new ShowUpdateDTO
        {
            StartAtUtc = new DateTime(2026, 5, 1, 20, 30, 0, DateTimeKind.Utc)
        };

        var response = await client.PutAsJsonAsync("/shows/1", dto);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    /// <summary>Verifica lo scenario di SH20_DeleteShow_DeletesEntity: predispone i dati e le condizioni previste dal caso di test e controlla che l'esito atteso venga restituito.</summary>
    [Fact]
    public async Task SH20_DeleteShow_DeletesEntity()
    {
        await _factory.ResetDatabaseAsync(db => SeedCinemaSalaFilmAndShowAsync(db));
        var client = _factory.CreateAdminClient();

        var response = await client.DeleteAsync("/shows/1");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var getResponse = await client.GetAsync("/shows/1");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    /// <summary>Verifica lo scenario di SH21_DeleteShow_NotFound: predispone i dati e le condizioni previste dal caso di test e controlla che l'esito atteso venga restituito.</summary>
    [Fact]
    public async Task SH21_DeleteShow_NotFound()
    {
        await _factory.ResetDatabaseAsync();
        var client = _factory.CreateAdminClient();

        var response = await client.DeleteAsync("/shows/999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>Verifica lo scenario di SH22_DeleteShow_BlockedByIssuedTickets: predispone i dati e le condizioni previste dal caso di test e controlla che l'esito atteso venga restituito.</summary>
    [Fact]
    public async Task SH22_DeleteShow_BlockedByIssuedTickets()
    {
        await _factory.ResetDatabaseAsync(db => SeedCinemaSalaFilmShowAndTicketAsync(db));
        var client = _factory.CreateAdminClient();

        var response = await client.DeleteAsync("/shows/1");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    /// <summary>Verifica lo scenario di SH23_CreateShow_ExactBoundaryNoOverlap: predispone i dati e le condizioni previste dal caso di test e controlla che l'esito atteso venga restituito.</summary>
    [Fact]
    public async Task SH23_CreateShow_ExactBoundaryNoOverlap()
    {
        await _factory.ResetDatabaseAsync(db => SeedCinemaSalaFilmAndShowAsync(db));
        var client = _factory.CreateAdminClient();

        var dto = new ShowCreateDTO
        {
            CinemaId = 1,
            SalaId = 1,
            FilmId = 1,
            StartAtUtc = new DateTime(2026, 5, 1, 22, 0, 0, DateTimeKind.Utc)
        };

        var response = await client.PostAsJsonAsync("/shows", dto);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    /// <summary>Verifica lo scenario di SH24_GetShowsByDate_FiltersByDate: predispone i dati e le condizioni previste dal caso di test e controlla che l'esito atteso venga restituito.</summary>
    [Fact]
    public async Task SH24_GetShowsByDate_FiltersByDate()
    {
        await _factory.ResetDatabaseAsync(db => SeedCinemaSalaFilmAndShowAsync(db));
        var client = _factory.CreateAnonymousClient();

        var response = await client.GetAsync("/shows?date=2026-05-01");

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<ShowPagedResultDTO>();
        Assert.NotNull(payload);
        Assert.Single(payload.Items);
    }

    /// <summary>Verifica lo scenario di SH25_GetShowsByDate_NoResultsForDifferentDate: predispone i dati e le condizioni previste dal caso di test e controlla che l'esito atteso venga restituito.</summary>
    [Fact]
    public async Task SH25_GetShowsByDate_NoResultsForDifferentDate()
    {
        await _factory.ResetDatabaseAsync(db => SeedCinemaSalaFilmAndShowAsync(db));
        var client = _factory.CreateAnonymousClient();

        var response = await client.GetAsync("/shows?date=2026-06-15");

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<ShowPagedResultDTO>();
        Assert.NotNull(payload);
        Assert.Empty(payload.Items);
    }

    /// <summary>Verifica lo scenario di SH26_CreateShow_PowerUserAllowed: predispone i dati e le condizioni previste dal caso di test e controlla che l'esito atteso venga restituito.</summary>
    [Fact]
    public async Task SH26_CreateShow_PowerUserAllowed()
    {
        await _factory.ResetDatabaseAsync(db => SeedCinemaSalaFilmAsync(db));
        var client = _factory.CreatePowerUserClient();

        var dto = new ShowCreateDTO
        {
            CinemaId = 1,
            SalaId = 1,
            FilmId = 1,
            StartAtUtc = new DateTime(2026, 5, 1, 20, 0, 0, DateTimeKind.Utc)
        };

        var response = await client.PostAsJsonAsync("/shows", dto);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    /// <summary>Verifica lo scenario di SH27_UpdateShow_PowerUserAllowed: predispone i dati e le condizioni previste dal caso di test e controlla che l'esito atteso venga restituito.</summary>
    [Fact]
    public async Task SH27_UpdateShow_PowerUserAllowed()
    {
        await _factory.ResetDatabaseAsync(db => SeedCinemaSalaFilmAndShowAsync(db));
        var client = _factory.CreatePowerUserClient();

        var dto = new ShowUpdateDTO { PrezzoBase = 11m };

        var response = await client.PutAsJsonAsync("/shows/1", dto);

        response.EnsureSuccessStatusCode();
    }

    /// <summary>Verifica lo scenario di SH28_DeleteShow_PowerUserAllowed: predispone i dati e le condizioni previste dal caso di test e controlla che l'esito atteso venga restituito.</summary>
    [Fact]
    public async Task SH28_DeleteShow_PowerUserAllowed()
    {
        await _factory.ResetDatabaseAsync(db => SeedCinemaSalaFilmAndShowAsync(db));
        var client = _factory.CreatePowerUserClient();

        var response = await client.DeleteAsync("/shows/1");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    private static async Task SeedCinemaAsync(FilmAPI.Data.FilmDbContext db)
    {
        var cinema = new Cinema { Nome = "Cinema Test", Citta = "Roma", Indirizzo = "Via Test 1" };
        db.Cinemas.Add(cinema);
        await db.SaveChangesAsync();
    }

    private static async Task SeedCinemaSalaAsync(FilmAPI.Data.FilmDbContext db)
    {
        await SeedCinemaAsync(db);

        var sala = new Sala
        {
            CinemaId = 1,
            NumeroProgressivo = 1,
            TipoSala = TipoSala.DueD,
            Nome = "Sala 1",
            Supplemento = 0,
            IsAttiva = true
        };
        db.Sale.Add(sala);
        await db.SaveChangesAsync();
    }

    private static async Task SeedCinemaSalaFilmAsync(FilmAPI.Data.FilmDbContext db)
    {
        await SeedCinemaSalaAsync(db);

        var regista = new Regista { Nome = "Test", Cognome = "Regista" };
        db.Registi.Add(regista);
        await db.SaveChangesAsync();

        var film = new Film
        {
            Titolo = "Film Test",
            Durata = 120,
            RegistaId = 1,
            DataProduzione = new DateTime(2024, 1, 1)
        };
        db.Films.Add(film);
        await db.SaveChangesAsync();
    }

    private static async Task SeedCinemaSalaFilmAndShowAsync(FilmAPI.Data.FilmDbContext db)
    {
        await SeedCinemaSalaFilmAsync(db);

        var show = new Show
        {
            CinemaId = 1,
            SalaId = 1,
            FilmId = 1,
            StartAtUtc = new DateTime(2026, 5, 1, 20, 0, 0, DateTimeKind.Utc),
            DurataMinutiSnapshot = 120,
            PrezzoBase = 10,
            SupplementoSala = 0
        };
        db.Shows.Add(show);
        await db.SaveChangesAsync();
    }

    private static async Task SeedCinemaSalaFilmAndTwoShowsAsync(FilmAPI.Data.FilmDbContext db)
    {
        await SeedCinemaSalaFilmAsync(db);

        var show1 = new Show
        {
            CinemaId = 1,
            SalaId = 1,
            FilmId = 1,
            StartAtUtc = new DateTime(2026, 5, 1, 18, 0, 0, DateTimeKind.Utc),
            DurataMinutiSnapshot = 90,
            PrezzoBase = 10,
            SupplementoSala = 0
        };
        var show2 = new Show
        {
            CinemaId = 1,
            SalaId = 1,
            FilmId = 1,
            StartAtUtc = new DateTime(2026, 5, 1, 21, 0, 0, DateTimeKind.Utc),
            DurataMinutiSnapshot = 120,
            PrezzoBase = 10,
            SupplementoSala = 0
        };
        db.Shows.AddRange(show1, show2);
        await db.SaveChangesAsync();
    }

    private static async Task SeedCinemaWithTwoSaleAndShowAsync(FilmAPI.Data.FilmDbContext db)
    {
        await SeedCinemaSalaFilmAsync(db);

        var sala2 = new Sala
        {
            CinemaId = 1,
            NumeroProgressivo = 2,
            TipoSala = TipoSala.TreD,
            Nome = "Sala 2",
            Supplemento = 2,
            IsAttiva = true
        };
        db.Sale.Add(sala2);
        await db.SaveChangesAsync();

        var show = new Show
        {
            CinemaId = 1,
            SalaId = 1,
            FilmId = 1,
            StartAtUtc = new DateTime(2026, 5, 1, 20, 0, 0, DateTimeKind.Utc),
            DurataMinutiSnapshot = 120,
            PrezzoBase = 10,
            SupplementoSala = 0
        };
        db.Shows.Add(show);
        await db.SaveChangesAsync();
    }

    private static async Task SeedTwoCinemasWithSaleAsync(FilmAPI.Data.FilmDbContext db)
    {
        await SeedCinemaSalaFilmAsync(db);

        var cinema2 = new Cinema { Nome = "Cinema Due", Citta = "Milano", Indirizzo = "Via Due 1" };
        db.Cinemas.Add(cinema2);
        await db.SaveChangesAsync();

        var sala2 = new Sala
        {
            CinemaId = cinema2.Id,
            NumeroProgressivo = 1,
            TipoSala = TipoSala.DueD,
            Nome = "Sala 1",
            Supplemento = 0,
            IsAttiva = true
        };
        db.Sale.Add(sala2);
        await db.SaveChangesAsync();
    }

    private static async Task SeedCinemaSalaFilmShowAndTicketAsync(FilmAPI.Data.FilmDbContext db)
    {
        var cinema = new Cinema { Nome = "Cinema Test", Citta = "Roma", Indirizzo = "Via Test 1" };
        db.Cinemas.Add(cinema);
        await db.SaveChangesAsync();

        var sala = new Sala
        {
            CinemaId = cinema.Id,
            NumeroProgressivo = 1,
            TipoSala = TipoSala.DueD,
            Nome = "Sala 1",
            Supplemento = 0,
            IsAttiva = true
        };
        db.Sale.Add(sala);
        await db.SaveChangesAsync();

        var posto = new SalaPosto { SalaId = sala.Id, Settore = "PLATEA", Fila = 1, Numero = 1 };
        db.SalaPosti.Add(posto);
        await db.SaveChangesAsync();

        var regista = new Regista { Nome = "Test", Cognome = "Regista" };
        db.Registi.Add(regista);
        await db.SaveChangesAsync();

        var film = new Film
        {
            Titolo = "Film Test",
            Durata = 120,
            RegistaId = regista.Id,
            DataProduzione = new DateTime(2024, 1, 1)
        };
        db.Films.Add(film);
        await db.SaveChangesAsync();

        var user = new User
        {
            Email = "ticket@test.com",
            PasswordHash = "hash",
            Nome = "Test",
            Cognome = "User",
            Ruolo = UserRole.User,
            DataRegistrazione = DateTime.UtcNow,
            CreditoResiduo = 0
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var show = new Show
        {
            CinemaId = cinema.Id,
            SalaId = sala.Id,
            FilmId = film.Id,
            StartAtUtc = DateTime.UtcNow.AddDays(-1),
            DurataMinutiSnapshot = 120,
            PrezzoBase = 10,
            SupplementoSala = 0
        };
        db.Shows.Add(show);
        await db.SaveChangesAsync();

        var ordine = new Ordine
        {
            CodiceOrdine = "ORD-SHOW-001",
            UserId = user.Id,
            ShowId = show.Id,
            CinemaId = cinema.Id,
            SalaId = sala.Id,
            FilmId = film.Id,
            HoldToken = "test-token",
            NumeroBiglietti = 1,
            TotaleLordo = 10,
            ImportoCredito = 0,
            ImportoCarta = 10,
            Stato = OrdineState.Paid,
            CreatedAtUtc = DateTime.UtcNow,
            PaidAtUtc = DateTime.UtcNow
        };
        db.Ordini.Add(ordine);
        await db.SaveChangesAsync();

        var biglietto = new Biglietto
        {
            OrdineId = ordine.Id,
            ShowId = show.Id,
            SalaPostoId = posto.Id,
            UserId = user.Id,
            CodiceBiglietto = "TKT-SHOW-001",
            BarcodeValue = "BC-SHOW-001",
            PrezzoBase = 10,
            Supplemento = 0,
            PrezzoTotale = 10,
            Stato = BigliettoState.Issued
        };
        db.Biglietti.Add(biglietto);
        await db.SaveChangesAsync();
    }
}
