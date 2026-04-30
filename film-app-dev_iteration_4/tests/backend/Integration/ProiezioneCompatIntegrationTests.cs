using System.Net;
using System.Net.Http.Json;
using FilmAPI.DTO;
using FilmAPI.Model;

namespace FilmAPI.Tests.Integration;

public class ProiezioneCompatIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ProiezioneCompatIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task PC1_GetProiezioni_ReadsFromShows()
    {
        await _factory.ResetDatabaseAsync(db => SeedCinemaSalaFilmAndShowAsync(db));
        var client = _factory.CreateAnonymousClient();

        var response = await client.GetAsync("/proiezioni");

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<List<ProiezioneDTO>>();
        Assert.NotNull(payload);
        Assert.Single(payload);
        Assert.Equal(1, payload[0].CinemaId);
        Assert.Equal(1, payload[0].FilmId);
    }

    [Fact]
    public async Task PC2_GetProiezioneById_ReadsFromShows()
    {
        await _factory.ResetDatabaseAsync(db => SeedCinemaSalaFilmAndShowAsync(db));
        var client = _factory.CreateAnonymousClient();

        var response = await client.GetAsync("/proiezioni/1");

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<ProiezioneDTO>();
        Assert.NotNull(payload);
        Assert.Equal(1, payload.Id);
    }

    [Fact]
    public async Task PC3_GetProiezioneById_NotFound()
    {
        await _factory.ResetDatabaseAsync();
        var client = _factory.CreateAnonymousClient();

        var response = await client.GetAsync("/proiezioni/999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PC4_CreateProiezione_CreatesShowViaBridge()
    {
        await _factory.ResetDatabaseAsync(db => SeedCinemaSalaFilmAsync(db));
        var client = _factory.CreateAdminClient();

        var dto = new ProiezioneCreateDTO
        {
            CinemaId = 1,
            FilmId = 1,
            Data = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc),
            Ora = new DateTime(1, 1, 1, 20, 0, 0, DateTimeKind.Utc)
        };

        var response = await client.PostAsJsonAsync("/proiezioni", dto);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ProiezioneDTO>();
        Assert.NotNull(payload);
        Assert.Equal(1, payload.CinemaId);
        Assert.Equal(1, payload.FilmId);
    }

    [Fact]
    public async Task PC5_CreateProiezione_FailsIfNoSala()
    {
        await _factory.ResetDatabaseAsync(db => SeedCinemaAndFilmOnlyAsync(db));
        var client = _factory.CreateAdminClient();

        var dto = new ProiezioneCreateDTO
        {
            CinemaId = 1,
            FilmId = 1,
            Data = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc),
            Ora = new DateTime(1, 1, 1, 20, 0, 0, DateTimeKind.Utc)
        };

        var response = await client.PostAsJsonAsync("/proiezioni", dto);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PC6_CreateProiezione_ConflictOnOverlap()
    {
        await _factory.ResetDatabaseAsync(db => SeedCinemaSalaFilmAndShowAsync(db));
        var client = _factory.CreateAdminClient();

        var dto = new ProiezioneCreateDTO
        {
            CinemaId = 1,
            FilmId = 1,
            Data = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc),
            Ora = new DateTime(1, 1, 1, 20, 30, 0, DateTimeKind.Utc)
        };

        var response = await client.PostAsJsonAsync("/proiezioni", dto);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task PC7_UpdateProiezione_UpdatesShowViaBridge()
    {
        await _factory.ResetDatabaseAsync(db => SeedCinemaSalaFilmAndShowAsync(db));
        var client = _factory.CreateAdminClient();

        var dto = new ProiezioneUpdateDTO
        {
            CinemaId = 1,
            FilmId = 1,
            Data = new DateTime(2026, 5, 2, 0, 0, 0, DateTimeKind.Utc),
            Ora = new DateTime(1, 1, 1, 21, 0, 0, DateTimeKind.Utc)
        };

        var response = await client.PutAsJsonAsync("/proiezioni/1", dto);

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<ProiezioneDTO>();
        Assert.NotNull(payload);
        Assert.Equal(1, payload.CinemaId);
        Assert.Equal(1, payload.FilmId);
    }

    [Fact]
    public async Task PC8_DeleteProiezione_DeletesShowViaBridge()
    {
        await _factory.ResetDatabaseAsync(db => SeedCinemaSalaFilmAndShowAsync(db));
        var client = _factory.CreateAdminClient();

        var response = await client.DeleteAsync("/proiezioni/1");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var getResponse = await client.GetAsync("/proiezioni/1");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task PC9_CreateProiezione_ForbiddenForUser()
    {
        await _factory.ResetDatabaseAsync(db => SeedCinemaSalaFilmAsync(db));
        var client = _factory.CreateUserClient();

        var dto = new ProiezioneCreateDTO
        {
            CinemaId = 1,
            FilmId = 1,
            Data = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc),
            Ora = new DateTime(1, 1, 1, 20, 0, 0, DateTimeKind.Utc)
        };

        var response = await client.PostAsJsonAsync("/proiezioni", dto);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task PC10_GetProiezioniPaged_ReadsFromShows()
    {
        await _factory.ResetDatabaseAsync(db => SeedCinemaSalaFilmAndShowAsync(db));
        var client = _factory.CreateAnonymousClient();

        var response = await client.GetAsync("/proiezioni?page=1&pageSize=10");

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<ProiezionePagedResultDTO>();
        Assert.NotNull(payload);
        Assert.Single(payload.Items);
        Assert.Equal(1, payload.TotalCount);
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

    private static async Task SeedCinemaAndFilmOnlyAsync(FilmAPI.Data.FilmDbContext db)
    {
        await SeedCinemaAsync(db);

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
}
