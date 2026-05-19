using System.Net;
using System.Net.Http.Json;
using FilmAPI.DTO;
using FilmAPI.Model;
using Microsoft.EntityFrameworkCore;

namespace FilmAPI.Tests.Integration;

/// <summary>Suite di test per SalaIntegrationTests.</summary>
public class SalaIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public SalaIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    /// <summary>Verifica lo scenario di S1_GetSaleByCinema_ReturnsEmptyList: predispone i dati e le condizioni previste dal caso di test e controlla che l'esito atteso venga restituito.</summary>
    [Fact]
    public async Task S1_GetSaleByCinema_ReturnsEmptyList()
    {
        await _factory.ResetDatabaseAsync();
        var client = _factory.CreateAnonymousClient();

        var response = await client.GetAsync("/cinemas/1/sale");

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<List<SalaDTO>>();
        Assert.NotNull(payload);
        Assert.Empty(payload);
    }

    /// <summary>Verifica lo scenario di S2_GetSaleByCinema_ReturnsSaleForCinema: predispone i dati e le condizioni previste dal caso di test e controlla che l'esito atteso venga restituito.</summary>
    [Fact]
    public async Task S2_GetSaleByCinema_ReturnsSaleForCinema()
    {
        await _factory.ResetDatabaseAsync(db => SeedCinemaAndSaleAsync(db));
        var client = _factory.CreateAnonymousClient();

        var response = await client.GetAsync("/cinemas/1/sale");

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<List<SalaDTO>>();
        Assert.NotNull(payload);
        Assert.Single(payload);
        Assert.Equal(1, payload[0].NumeroProgressivo);
        Assert.Equal(TipoSala.DueD, payload[0].TipoSala);
    }

    /// <summary>Verifica lo scenario di S3_GetSalaById_ReturnsSala: predispone i dati e le condizioni previste dal caso di test e controlla che l'esito atteso venga restituito.</summary>
    [Fact]
    public async Task S3_GetSalaById_ReturnsSala()
    {
        await _factory.ResetDatabaseAsync(db => SeedCinemaAndSaleAsync(db));
        var client = _factory.CreateAnonymousClient();

        var response = await client.GetAsync("/sale/1");

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<SalaDTO>();
        Assert.NotNull(payload);
        Assert.Equal(1, payload.Id);
        Assert.Equal(1, payload.CinemaId);
        Assert.Equal(1, payload.NumeroProgressivo);
    }

    /// <summary>Verifica lo scenario di S4_GetSalaById_NotFound: predispone i dati e le condizioni previste dal caso di test e controlla che l'esito atteso venga restituito.</summary>
    [Fact]
    public async Task S4_GetSalaById_NotFound()
    {
        await _factory.ResetDatabaseAsync();
        var client = _factory.CreateAnonymousClient();

        var response = await client.GetAsync("/sale/999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>Verifica lo scenario di S5_CreateSala_ReturnsCreated: predispone i dati e le condizioni previste dal caso di test e controlla che l'esito atteso venga restituito.</summary>
    [Fact]
    public async Task S5_CreateSala_ReturnsCreated()
    {
        await _factory.ResetDatabaseAsync(db => SeedCinemaAsync(db));
        var client = _factory.CreateAdminClient();

        var dto = new SalaCreateDTO
        {
            CinemaId = 1,
            NumeroProgressivo = 1,
            TipoSala = TipoSala.DueD,
            Supplemento = 0
        };

        var response = await client.PostAsJsonAsync("/cinemas/1/sale", dto);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<SalaDTO>();
        Assert.NotNull(payload);
        Assert.Equal(1, payload.CinemaId);
        Assert.Equal(1, payload.NumeroProgressivo);
        Assert.Equal("Sala 1", payload.Nome);
    }

    /// <summary>Verifica lo scenario di S6_CreateSala_ConflictOnDuplicateNumero: predispone i dati e le condizioni previste dal caso di test e controlla che l'esito atteso venga restituito.</summary>
    [Fact]
    public async Task S6_CreateSala_ConflictOnDuplicateNumero()
    {
        await _factory.ResetDatabaseAsync(db => SeedCinemaAndSaleAsync(db));
        var client = _factory.CreateAdminClient();

        var dto = new SalaCreateDTO
        {
            CinemaId = 1,
            NumeroProgressivo = 1,
            TipoSala = TipoSala.TreD,
            Supplemento = 2
        };

        var response = await client.PostAsJsonAsync("/cinemas/1/sale", dto);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    /// <summary>Verifica lo scenario di S7_CreateSala_BadRequestOnInvalidCinema: predispone i dati e le condizioni previste dal caso di test e controlla che l'esito atteso venga restituito.</summary>
    [Fact]
    public async Task S7_CreateSala_BadRequestOnInvalidCinema()
    {
        await _factory.ResetDatabaseAsync();
        var client = _factory.CreateAdminClient();

        var dto = new SalaCreateDTO
        {
            CinemaId = 999,
            NumeroProgressivo = 1,
            TipoSala = TipoSala.DueD,
            Supplemento = 0
        };

        var response = await client.PostAsJsonAsync("/cinemas/999/sale", dto);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>Verifica lo scenario di S8_CreateSala_ForbiddenForUser: predispone i dati e le condizioni previste dal caso di test e controlla che l'esito atteso venga restituito.</summary>
    [Fact]
    public async Task S8_CreateSala_ForbiddenForUser()
    {
        await _factory.ResetDatabaseAsync(db => SeedCinemaAsync(db));
        var client = _factory.CreateUserClient();

        var dto = new SalaCreateDTO
        {
            CinemaId = 1,
            NumeroProgressivo = 1,
            TipoSala = TipoSala.DueD,
            Supplemento = 0
        };

        var response = await client.PostAsJsonAsync("/cinemas/1/sale", dto);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    /// <summary>Verifica lo scenario di S9_UpdateSala_UpdatesFields: predispone i dati e le condizioni previste dal caso di test e controlla che l'esito atteso venga restituito.</summary>
    [Fact]
    public async Task S9_UpdateSala_UpdatesFields()
    {
        await _factory.ResetDatabaseAsync(db => SeedCinemaAndSaleAsync(db));
        var client = _factory.CreateAdminClient();

        var dto = new SalaUpdateDTO
        {
            TipoSala = TipoSala.ISENSE,
            Nome = "Sala Premium",
            Supplemento = 3.50m,
            IsAttiva = true
        };

        var response = await client.PutAsJsonAsync("/sale/1", dto);

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<SalaDTO>();
        Assert.NotNull(payload);
        Assert.Equal(TipoSala.ISENSE, payload.TipoSala);
        Assert.Equal("Sala Premium", payload.Nome);
        Assert.Equal(3.50m, payload.Supplemento);
    }

    /// <summary>Verifica lo scenario di S10_UpdateSala_NotFound: predispone i dati e le condizioni previste dal caso di test e controlla che l'esito atteso venga restituito.</summary>
    [Fact]
    public async Task S10_UpdateSala_NotFound()
    {
        await _factory.ResetDatabaseAsync();
        var client = _factory.CreateAdminClient();

        var dto = new SalaUpdateDTO
        {
            TipoSala = TipoSala.DueD,
            Supplemento = 0,
            IsAttiva = true
        };

        var response = await client.PutAsJsonAsync("/sale/999", dto);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>Verifica lo scenario di S11_DeleteSala_DeletesEntity: predispone i dati e le condizioni previste dal caso di test e controlla che l'esito atteso venga restituito.</summary>
    [Fact]
    public async Task S11_DeleteSala_DeletesEntity()
    {
        await _factory.ResetDatabaseAsync(db => SeedCinemaAndSaleAsync(db));
        var client = _factory.CreateAdminClient();

        var response = await client.DeleteAsync("/sale/1");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var getResponse = await client.GetAsync("/sale/1");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    /// <summary>Verifica lo scenario di S12_DeleteSala_NotFound: predispone i dati e le condizioni previste dal caso di test e controlla che l'esito atteso venga restituito.</summary>
    [Fact]
    public async Task S12_DeleteSala_NotFound()
    {
        await _factory.ResetDatabaseAsync();
        var client = _factory.CreateAdminClient();

        var response = await client.DeleteAsync("/sale/999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>Verifica lo scenario di S13_DeleteSala_BlockedByFutureShows: predispone i dati e le condizioni previste dal caso di test e controlla che l'esito atteso venga restituito.</summary>
    [Fact]
    public async Task S13_DeleteSala_BlockedByFutureShows()
    {
        await _factory.ResetDatabaseAsync(db => SeedCinemaSalaAndFutureShowAsync(db));
        var client = _factory.CreateAdminClient();

        var response = await client.DeleteAsync("/sale/1");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    /// <summary>Verifica lo scenario di S14_GetPosti_ReturnsEmptyList: predispone i dati e le condizioni previste dal caso di test e controlla che l'esito atteso venga restituito.</summary>
    [Fact]
    public async Task S14_GetPosti_ReturnsEmptyList()
    {
        await _factory.ResetDatabaseAsync(db => SeedCinemaAndSaleAsync(db));
        var client = _factory.CreateAnonymousClient();

        var response = await client.GetAsync("/sale/1/posti");

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<List<SalaPostoDTO>>();
        Assert.NotNull(payload);
        Assert.Empty(payload);
    }

    /// <summary>Verifica lo scenario di S15_GetPosti_NotFound: predispone i dati e le condizioni previste dal caso di test e controlla che l'esito atteso venga restituito.</summary>
    [Fact]
    public async Task S15_GetPosti_NotFound()
    {
        await _factory.ResetDatabaseAsync();
        var client = _factory.CreateAnonymousClient();

        var response = await client.GetAsync("/sale/999/posti");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>Verifica lo scenario di S16_SavePosti_CreatesLayout: predispone i dati e le condizioni previste dal caso di test e controlla che l'esito atteso venga restituito.</summary>
    [Fact]
    public async Task S16_SavePosti_CreatesLayout()
    {
        await _factory.ResetDatabaseAsync(db => SeedCinemaAndSaleAsync(db));
        var client = _factory.CreateAdminClient();

        var dto = new SalaLayoutSaveDTO
        {
            Posti = new List<SalaPostoDTO>
            {
                new() { Settore = "PLATEA", Fila = 1, Numero = 1, PosX = 0, PosY = 0 },
                new() { Settore = "PLATEA", Fila = 1, Numero = 2, PosX = 1, PosY = 0 },
                new() { Settore = "PLATEA", Fila = 2, Numero = 1, PosX = 0, PosY = 1 },
            }
        };

        var response = await client.PutAsJsonAsync("/sale/1/posti", dto);

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<List<SalaPostoDTO>>();
        Assert.NotNull(payload);
        Assert.Equal(3, payload.Count);
        Assert.Equal("PLATEA", payload[0].Settore);
        Assert.Equal(1, payload[0].Fila);
        Assert.Equal(1, payload[0].Numero);
    }

    /// <summary>Verifica lo scenario di S17_SavePosti_ReplacesExistingLayout: predispone i dati e le condizioni previste dal caso di test e controlla che l'esito atteso venga restituito.</summary>
    [Fact]
    public async Task S17_SavePosti_ReplacesExistingLayout()
    {
        await _factory.ResetDatabaseAsync(db => SeedCinemaSalaWithPostiAsync(db));
        var client = _factory.CreateAdminClient();

        var dto = new SalaLayoutSaveDTO
        {
            Posti = new List<SalaPostoDTO>
            {
                new() { Settore = "BALCONE", Fila = 1, Numero = 1 },
            }
        };

        var response = await client.PutAsJsonAsync("/sale/1/posti", dto);

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<List<SalaPostoDTO>>();
        Assert.NotNull(payload);
        Assert.Single(payload);
        Assert.Equal("BALCONE", payload[0].Settore);
    }

    /// <summary>Verifica lo scenario di S18_SavePosti_NotFound: predispone i dati e le condizioni previste dal caso di test e controlla che l'esito atteso venga restituito.</summary>
    [Fact]
    public async Task S18_SavePosti_NotFound()
    {
        await _factory.ResetDatabaseAsync();
        var client = _factory.CreateAdminClient();

        var dto = new SalaLayoutSaveDTO { Posti = new List<SalaPostoDTO>() };

        var response = await client.PutAsJsonAsync("/sale/999/posti", dto);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>Verifica lo scenario di S19_SavePosti_ForbiddenForUser: predispone i dati e le condizioni previste dal caso di test e controlla che l'esito atteso venga restituito.</summary>
    [Fact]
    public async Task S19_SavePosti_ForbiddenForUser()
    {
        await _factory.ResetDatabaseAsync(db => SeedCinemaAndSaleAsync(db));
        var client = _factory.CreateUserClient();

        var dto = new SalaLayoutSaveDTO { Posti = new List<SalaPostoDTO>() };

        var response = await client.PutAsJsonAsync("/sale/1/posti", dto);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    /// <summary>Verifica lo scenario di S20_DeleteSala_BlockedByIssuedTickets: predispone i dati e le condizioni previste dal caso di test e controlla che l'esito atteso venga restituito.</summary>
    [Fact]
    public async Task S20_DeleteSala_BlockedByIssuedTickets()
    {
        await _factory.ResetDatabaseAsync(db => SeedCinemaSalaWithTicketsAsync(db));
        var client = _factory.CreateAdminClient();

        var response = await client.DeleteAsync("/sale/1");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    private static async Task SeedCinemaAsync(FilmAPI.Data.FilmDbContext db)
    {
        var cinema = new Cinema { Nome = "Cinema Test", Citta = "Roma", Indirizzo = "Via Test 1" };
        db.Cinemas.Add(cinema);
        await db.SaveChangesAsync();
    }

    private static async Task SeedCinemaAndSaleAsync(FilmAPI.Data.FilmDbContext db)
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

    private static async Task SeedCinemaSalaWithPostiAsync(FilmAPI.Data.FilmDbContext db)
    {
        await SeedCinemaAndSaleAsync(db);

        db.SalaPosti.Add(new SalaPosto { SalaId = 1, Settore = "PLATEA", Fila = 1, Numero = 1 });
        db.SalaPosti.Add(new SalaPosto { SalaId = 1, Settore = "PLATEA", Fila = 1, Numero = 2 });
        await db.SaveChangesAsync();
    }

    private static async Task SeedCinemaSalaAndFutureShowAsync(FilmAPI.Data.FilmDbContext db)
    {
        await SeedCinemaAndSaleAsync(db);

        var regista = new Regista { Nome = "Test", Cognome = "Regista" };
        db.Registi.Add(regista);
        await db.SaveChangesAsync();

        var film = new Film { Titolo = "Film Test", Durata = 120, RegistaId = 1, DataProduzione = new DateTime(2024, 1, 1) };
        db.Films.Add(film);
        await db.SaveChangesAsync();

        var show = new Show
        {
            CinemaId = 1,
            SalaId = 1,
            FilmId = 1,
            StartAtUtc = DateTime.UtcNow.AddDays(7),
            DurataMinutiSnapshot = 120,
            PrezzoBase = 10,
            SupplementoSala = 0
        };
        db.Shows.Add(show);
        await db.SaveChangesAsync();
    }

    private static async Task SeedCinemaSalaWithTicketsAsync(FilmAPI.Data.FilmDbContext db)
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

        var posto1 = new SalaPosto { SalaId = sala.Id, Settore = "PLATEA", Fila = 1, Numero = 1 };
        var posto2 = new SalaPosto { SalaId = sala.Id, Settore = "PLATEA", Fila = 1, Numero = 2 };
        db.SalaPosti.AddRange(posto1, posto2);
        await db.SaveChangesAsync();

        var regista = new Regista { Nome = "Test", Cognome = "Regista" };
        db.Registi.Add(regista);
        await db.SaveChangesAsync();

        var film = new Film { Titolo = "Film Test", Durata = 120, RegistaId = regista.Id, DataProduzione = new DateTime(2024, 1, 1) };
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
            CodiceOrdine = "ORD-TEST-001",
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
            SalaPostoId = posto1.Id,
            UserId = user.Id,
            CodiceBiglietto = "TKT-001",
            BarcodeValue = "BC-001",
            PrezzoBase = 10,
            Supplemento = 0,
            PrezzoTotale = 10,
            Stato = BigliettoState.Issued
        };
        db.Biglietti.Add(biglietto);
        await db.SaveChangesAsync();
    }
}
