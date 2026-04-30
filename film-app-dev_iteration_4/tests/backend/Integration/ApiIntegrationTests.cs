using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FilmAPI.DTO;
using FilmAPI.Model;

namespace FilmAPI.Tests.Integration;

public class ApiIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ApiIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task R1_GetRegisti_ReturnsEmptyList()
    {
        await _factory.ResetDatabaseAsync();
        var client = _factory.CreatePowerUserClient();

        var response = await client.GetAsync("/registi/");

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<List<RegistaDTO>>();
        Assert.NotNull(payload);
        Assert.Empty(payload);
    }

    [Fact]
    public async Task R2_PostRegisti_CreatesEntity_AndReturnsCreated()
    {
        await _factory.ResetDatabaseAsync();
        var client = _factory.CreatePowerUserClient();

        var request = new RegistaCreateDTO
        {
            Nome = "Christopher",
            Cognome = "Nolan",
            Nazionalita = "UK"
        };

        var response = await client.PostAsJsonAsync("/registi/", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<RegistaDTO>();
        Assert.NotNull(payload);
        Assert.True(payload.Id > 0);
        Assert.Equal("Christopher", payload.Nome);
    }

    [Fact]
    public async Task R3_GetRegistiById_ReturnsEntity_WhenExists()
    {
        await _factory.ResetDatabaseAsync();
        var client = _factory.CreatePowerUserClient();

        var created = await CreateRegistaAsync(client, "Martin", "Scorsese", "IT");
        var response = await client.GetAsync($"/registi/{created.Id}");

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<RegistaDTO>();
        Assert.NotNull(payload);
        Assert.Equal(created.Id, payload.Id);
        Assert.Equal("Martin", payload.Nome);
    }

    [Fact]
    public async Task R4_GetRegistiById_ReturnsNotFound_WhenMissing()
    {
        await _factory.ResetDatabaseAsync();
        var client = _factory.CreatePowerUserClient();

        var response = await client.GetAsync("/registi/99999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task R5_PutRegisti_UpdatesEntity_WhenExists()
    {
        await _factory.ResetDatabaseAsync();
        var client = _factory.CreatePowerUserClient();
        var created = await CreateRegistaAsync(client, "Christopher", "Nolan", "UK");

        var request = new RegistaUpdateDTO
        {
            Nome = "Christopher",
            Cognome = "Nolan",
            Nazionalita = "Statunitense"
        };

        var response = await client.PutAsJsonAsync($"/registi/{created.Id}", request);

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<RegistaDTO>();
        Assert.NotNull(payload);
        Assert.Equal("Statunitense", payload.Nazionalita);
    }

    [Fact]
    public async Task R6_PutRegisti_ReturnsNotFound_WhenMissing()
    {
        await _factory.ResetDatabaseAsync();
        var client = _factory.CreatePowerUserClient();

        var request = new RegistaUpdateDTO
        {
            Nome = "Quentin",
            Cognome = "Tarantino",
            Nazionalita = "US"
        };

        var response = await client.PutAsJsonAsync("/registi/99999", request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task R7_DeleteRegisti_DeletesEntity_WhenExists()
    {
        await _factory.ResetDatabaseAsync();
        var client = _factory.CreatePowerUserClient();
        var created = await CreateRegistaAsync(client, "Ridley", "Scott", "UK");

        var deleteResponse = await client.DeleteAsync($"/registi/{created.Id}");
        var getResponse = await client.GetAsync($"/registi/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task R8_DeleteRegisti_ReturnsNotFound_WhenMissing()
    {
        await _factory.ResetDatabaseAsync();
        var client = _factory.CreatePowerUserClient();

        var response = await client.DeleteAsync("/registi/99999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task R9_PostRegisti_ReturnsBadRequest_WhenDataIsMissing()
    {
        await _factory.ResetDatabaseAsync();
        var client = _factory.CreatePowerUserClient();

        var request = new
        {
            Nome = "Christopher"
        };

        var response = await client.PostAsJsonAsync("/registi/", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task R10_GetRegisti_WithPaginationAndSearch_ReturnsPagedResult()
    {
        await _factory.ResetDatabaseAsync();
        var client = _factory.CreatePowerUserClient();

        _ = await CreateRegistaAsync(client, "Mario", "Rossi", "Italia");
        _ = await CreateRegistaAsync(client, "Lucia", "Verdi", "Italia");
        _ = await CreateRegistaAsync(client, "John", "Smith", "USA");

        var response = await client.GetAsync("/registi?page=1&pageSize=1&search=Italia");

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<RegistaPagedResultDTO>();
        Assert.NotNull(payload);
        Assert.Equal(1, payload.Page);
        Assert.Equal(1, payload.PageSize);
        Assert.Equal(2, payload.TotalCount);
        Assert.Equal(2, payload.TotalPages);
        Assert.Single(payload.Items);
        Assert.Equal("Italia", payload.Items[0].Nazionalita);
    }

    [Fact]
    public async Task R11_GetRegisti_WithoutPaginationParams_ReturnsLegacyArrayPayload()
    {
        await _factory.ResetDatabaseAsync();
        var client = _factory.CreatePowerUserClient();

        _ = await CreateRegistaAsync(client, "Paolo", "Sorrentino", "Italia");

        var response = await client.GetAsync("/registi");

        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(content);
        Assert.Equal(JsonValueKind.Array, json.RootElement.ValueKind);

        var payload = JsonSerializer.Deserialize<List<RegistaDTO>>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        Assert.NotNull(payload);
        Assert.Single(payload);
    }

    [Fact]
    public async Task F1_GetFilms_ReturnsEmptyList()
    {
        await _factory.ResetDatabaseAsync();
        var client = _factory.CreatePowerUserClient();

        var response = await client.GetAsync("/films/");

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<List<FilmDTO>>();
        Assert.NotNull(payload);
        Assert.Empty(payload);
    }

    [Fact]
    public async Task F2_PostFilms_CreatesEntity_WhenInputIsValid()
    {
        await _factory.ResetDatabaseAsync();
        var client = _factory.CreatePowerUserClient();
        var regista = await CreateRegistaAsync(client, "Christopher", "Nolan", "UK");

        var request = new FilmCreateDTO
        {
            Titolo = "Inception",
            DataProduzione = new DateTime(2010, 7, 16),
            RegistaId = regista.Id,
            Durata = 148,
            CopertinaPath = "/media/inception.jpg"
        };

        var response = await client.PostAsJsonAsync("/films/", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<FilmDTO>();
        Assert.NotNull(payload);
        Assert.Equal("Inception", payload.Titolo);
    }

    [Fact]
    public async Task F3_PostFilms_UsesDefaultCoverPath_WhenCopertinaPathIsMissing()
    {
        await _factory.ResetDatabaseAsync();
        var client = _factory.CreatePowerUserClient();
        var regista = await CreateRegistaAsync(client, "Denis", "Villeneuve", "CA");

        var request = new FilmCreateDTO
        {
            Titolo = "Interstellar",
            DataProduzione = new DateTime(2014, 11, 7),
            RegistaId = regista.Id,
            Durata = 169
        };

        var response = await client.PostAsJsonAsync("/films/", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<FilmDTO>();
        Assert.NotNull(payload);
        Assert.Equal("/media/defaults/cover-default.jpg", payload.CopertinaPath);
    }

    [Fact]
    public async Task F4_PostFilms_ReturnsBadRequest_WhenRegistaDoesNotExist()
    {
        await _factory.ResetDatabaseAsync();
        var client = _factory.CreatePowerUserClient();

        var request = new FilmCreateDTO
        {
            Titolo = "Dune",
            DataProduzione = new DateTime(2021, 10, 22),
            RegistaId = 999,
            Durata = 155
        };

        var response = await client.PostAsJsonAsync("/films/", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task F5_GetFilmsById_ReturnsEntity_WhenExists()
    {
        await _factory.ResetDatabaseAsync();
        var client = _factory.CreatePowerUserClient();
        var regista = await CreateRegistaAsync(client, "Hayao", "Miyazaki", "JP");
        var film = await CreateFilmAsync(client, regista.Id, "Spirited Away");

        var response = await client.GetAsync($"/films/{film.Id}");

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<FilmDTO>();
        Assert.NotNull(payload);
        Assert.Equal(film.Id, payload.Id);
        Assert.Equal("Spirited Away", payload.Titolo);
    }

    [Fact]
    public async Task F6_PutFilms_UpdatesEntity_WhenExists()
    {
        await _factory.ResetDatabaseAsync();
        var client = _factory.CreatePowerUserClient();
        var regista = await CreateRegistaAsync(client, "Wes", "Anderson", "US");
        var film = await CreateFilmAsync(client, regista.Id, "Old Title");

        var request = new FilmUpdateDTO
        {
            Titolo = "New Title",
            DataProduzione = new DateTime(2004, 1, 1),
            RegistaId = regista.Id,
            Durata = 120,
            CopertinaPath = "/media/new.jpg"
        };

        var response = await client.PutAsJsonAsync($"/films/{film.Id}", request);

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<FilmDTO>();
        Assert.NotNull(payload);
        Assert.Equal("New Title", payload.Titolo);
    }

    [Fact]
    public async Task F7_PutFilms_ReturnsBadRequest_WhenRegistaDoesNotExist()
    {
        await _factory.ResetDatabaseAsync();
        var client = _factory.CreatePowerUserClient();
        var regista = await CreateRegistaAsync(client, "Sofia", "Coppola", "US");
        var film = await CreateFilmAsync(client, regista.Id, "Lost in Translation");

        var request = new FilmUpdateDTO
        {
            Titolo = "Lost in Translation",
            DataProduzione = new DateTime(2003, 9, 12),
            RegistaId = 999,
            Durata = 102,
            CopertinaPath = "/media/lit.jpg",
            FilmatoPath = "/media/lit.mp4"
        };

        var response = await client.PutAsJsonAsync($"/films/{film.Id}", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task F8_DeleteFilms_DeletesEntity_WhenExists()
    {
        await _factory.ResetDatabaseAsync();
        var client = _factory.CreatePowerUserClient();
        var regista = await CreateRegistaAsync(client, "Patty", "Jenkins", "US");
        var film = await CreateFilmAsync(client, regista.Id, "Monster");

        var deleteResponse = await client.DeleteAsync($"/films/{film.Id}");
        var getResponse = await client.GetAsync($"/films/{film.Id}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task C1_GetCinemas_ReturnsEmptyList()
    {
        await _factory.ResetDatabaseAsync();
        var client = _factory.CreatePowerUserClient();

        var response = await client.GetAsync("/cinemas/");

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<List<CinemaDTO>>();
        Assert.NotNull(payload);
        Assert.Empty(payload);
    }

    [Fact]
    public async Task C2_PostCinemas_CreatesEntity_WhenInputIsValid()
    {
        await _factory.ResetDatabaseAsync();
        var client = _factory.CreateAdminClient();

        var request = new CinemaCreateDTO
        {
            Nome = "Cinema Odeon",
            Indirizzo = "Via Roma 10",
            Citta = "Milano"
        };

        var response = await client.PostAsJsonAsync("/cinemas/", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<CinemaDTO>();
        Assert.NotNull(payload);
        Assert.True(payload.Id > 0);
        Assert.Equal("Cinema Odeon", payload.Nome);
    }

    [Fact]
    public async Task C3_GetCinemasById_ReturnsEntity_WhenExists()
    {
        await _factory.ResetDatabaseAsync();
        var client = _factory.CreateAdminClient();
        var cinema = await CreateCinemaAsync(client, "Cinema Lumiere", "Via Po 1", "Torino");

        var response = await client.GetAsync($"/cinemas/{cinema.Id}");

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<CinemaDTO>();
        Assert.NotNull(payload);
        Assert.Equal(cinema.Id, payload.Id);
    }

    [Fact]
    public async Task C4_PutCinemas_UpdatesEntity_WhenExists()
    {
        await _factory.ResetDatabaseAsync();
        var client = _factory.CreateAdminClient();
        var cinema = await CreateCinemaAsync(client, "Cinema Vecchio", "Via A", "Roma");

        var request = new CinemaUpdateDTO
        {
            Nome = "Cinema Nuovo",
            Indirizzo = "Via B",
            Citta = "Roma"
        };

        var response = await client.PutAsJsonAsync($"/cinemas/{cinema.Id}", request);

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<CinemaDTO>();
        Assert.NotNull(payload);
        Assert.Equal("Cinema Nuovo", payload.Nome);
    }

    [Fact]
    public async Task C5_DeleteCinemas_DeletesEntity_WhenExists()
    {
        await _factory.ResetDatabaseAsync();
        var client = _factory.CreateAdminClient();
        var cinema = await CreateCinemaAsync(client, "Cinema Test", "Via Test 1", "Bologna");

        var deleteResponse = await client.DeleteAsync($"/cinemas/{cinema.Id}");
        var getResponse = await client.GetAsync($"/cinemas/{cinema.Id}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task C6_GetCinemas_WithPaginationAndSearch_ReturnsPagedResult()
    {
        await _factory.ResetDatabaseAsync();
        var adminClient = _factory.CreateAdminClient();
        var anonymousClient = _factory.CreatePowerUserClient();

        _ = await CreateCinemaAsync(adminClient, "Cinema Roma Centro", "Via Uno", "Roma");
        _ = await CreateCinemaAsync(adminClient, "Cinema Roma Nord", "Via Due", "Roma");
        _ = await CreateCinemaAsync(adminClient, "Cinema Milano", "Via Tre", "Milano");

        var response = await anonymousClient.GetAsync("/cinemas?page=1&pageSize=1&search=Roma");

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<CinemaPagedResultDTO>();
        Assert.NotNull(payload);
        Assert.Equal(1, payload.Page);
        Assert.Equal(1, payload.PageSize);
        Assert.Equal(2, payload.TotalCount);
        Assert.Equal(2, payload.TotalPages);
        Assert.Single(payload.Items);
        Assert.Contains("Roma", payload.Items[0].Nome);
    }

    [Fact]
    public async Task C7_GetCinemas_WithoutPaginationParams_ReturnsLegacyArrayPayload()
    {
        await _factory.ResetDatabaseAsync();
        var adminClient = _factory.CreateAdminClient();
        var client = _factory.CreatePowerUserClient();

        _ = await CreateCinemaAsync(adminClient, "Cinema Legacy", "Via Legacy 1", "Bari");

        var response = await client.GetAsync("/cinemas");

        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(content);
        Assert.Equal(JsonValueKind.Array, json.RootElement.ValueKind);

        var payload = JsonSerializer.Deserialize<List<CinemaDTO>>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        Assert.NotNull(payload);
        Assert.Single(payload);
    }

    [Fact]
    public async Task P1_GetProiezioni_ReturnsEmptyList()
    {
        await _factory.ResetDatabaseAsync();
        var client = _factory.CreatePowerUserClient();

        var response = await client.GetAsync("/proiezioni/");

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<List<ProiezioneDTO>>();
        Assert.NotNull(payload);
        Assert.Empty(payload);
    }

    [Fact]
    public async Task P2_PostProiezioni_CreatesEntity_WhenInputIsValid()
    {
        await _factory.ResetDatabaseAsync();
        var client = _factory.CreateAdminClient();
        var regista = await CreateRegistaAsync(client, "Jane", "Campion", "NZ");
        var film = await CreateFilmAsync(client, regista.Id, "The Piano");
        var cinema = await CreateCinemaAsync(client, "Arcadia", "Corso 10", "Torino");

        var salaRequest = new SalaCreateDTO
        {
            CinemaId = cinema.Id,
            NumeroProgressivo = 1,
            TipoSala = TipoSala.DueD,
            Supplemento = 0
        };
        var salaResponse = await client.PostAsJsonAsync($"/cinemas/{cinema.Id}/sale", salaRequest);
        salaResponse.EnsureSuccessStatusCode();

        var request = new ProiezioneCreateDTO
        {
            CinemaId = cinema.Id,
            FilmId = film.Id,
            Data = new DateTime(2024, 12, 25),
            Ora = new DateTime(2024, 12, 25, 20, 0, 0)
        };

        var response = await client.PostAsJsonAsync("/proiezioni/", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ProiezioneDTO>();
        Assert.NotNull(payload);
        Assert.Equal(cinema.Id, payload.CinemaId);
        Assert.Equal(film.Id, payload.FilmId);
    }

    [Fact]
    public async Task P3_PostProiezioni_ReturnsBadRequest_WhenCinemaDoesNotExist()
    {
        await _factory.ResetDatabaseAsync();
        var client = _factory.CreatePowerUserClient();
        var regista = await CreateRegistaAsync(client, "Yorgos", "Lanthimos", "GR");
        var film = await CreateFilmAsync(client, regista.Id, "Poor Things");

        var request = new ProiezioneCreateDTO
        {
            CinemaId = 999,
            FilmId = film.Id,
            Data = new DateTime(2026, 1, 10),
            Ora = new DateTime(2026, 1, 10, 20, 30, 0)
        };

        var response = await client.PostAsJsonAsync("/proiezioni/", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task P4_PostProiezioni_ReturnsBadRequest_WhenFilmDoesNotExist()
    {
        await _factory.ResetDatabaseAsync();
        var client = _factory.CreateAdminClient();
        var cinema = await CreateCinemaAsync(client, "Cinema Centrale", "Via Roma 1", "Milano");

        var request = new ProiezioneCreateDTO
        {
            CinemaId = cinema.Id,
            FilmId = 999,
            Data = new DateTime(2026, 1, 10),
            Ora = new DateTime(2026, 1, 10, 20, 30, 0)
        };

        var response = await client.PostAsJsonAsync("/proiezioni/", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task P5_PostProiezioni_ReturnsConflict_ForDuplicateUniqueTuple()
    {
        await _factory.ResetDatabaseAsync();
        var client = _factory.CreateAdminClient();
        var regista = await CreateRegistaAsync(client, "Peter", "Jackson", "NZ");
        var cinema = await CreateCinemaAsync(client, "Arcadia", "Corso 10", "Torino");
        var film = await CreateFilmAsync(client, regista.Id, "LOTR");
        var data = new DateTime(2026, 2, 1);
        var ora = new DateTime(2026, 2, 1, 21, 0, 0);

        await CreateProiezioneAsync(client, cinema.Id, film.Id, data, ora);

        var request = new ProiezioneCreateDTO
        {
            CinemaId = cinema.Id,
            FilmId = film.Id,
            Data = data,
            Ora = ora
        };

        var response = await client.PostAsJsonAsync("/proiezioni/", request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task P6_GetProiezioniById_ReturnsEntity_WhenExists()
    {
        await _factory.ResetDatabaseAsync();
        var client = _factory.CreateAdminClient();
        var regista = await CreateRegistaAsync(client, "Alfonso", "Cuaron", "MX");
        var film = await CreateFilmAsync(client, regista.Id, "Gravity");
        var cinema = await CreateCinemaAsync(client, "Cinema A", "Via A", "Roma");
        var proiezione = await CreateProiezioneAsync(client, cinema.Id, film.Id, new DateTime(2026, 3, 1), new DateTime(2026, 3, 1, 18, 0, 0));

        var response = await client.GetAsync($"/proiezioni/{proiezione.Id}");

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<ProiezioneDTO>();
        Assert.NotNull(payload);
        Assert.Equal(proiezione.Id, payload.Id);
    }

    [Fact]
    public async Task P7_PutProiezioni_UpdatesEntity_WhenExists()
    {
        await _factory.ResetDatabaseAsync();
        var client = _factory.CreateAdminClient();
        var regista = await CreateRegistaAsync(client, "Greta", "Gerwig", "US");
        var film1 = await CreateFilmAsync(client, regista.Id, "Lady Bird");
        var film2 = await CreateFilmAsync(client, regista.Id, "Barbie");
        var cinema1 = await CreateCinemaAsync(client, "Cinema 1", "Via Uno", "Milano");
        var cinema2 = await CreateCinemaAsync(client, "Cinema 2", "Via Due", "Milano");

        var sala2Request = new SalaCreateDTO
        {
            CinemaId = cinema2.Id,
            NumeroProgressivo = 1,
            TipoSala = TipoSala.DueD,
            Supplemento = 0
        };
        var sala2Response = await client.PostAsJsonAsync($"/cinemas/{cinema2.Id}/sale", sala2Request);
        sala2Response.EnsureSuccessStatusCode();

        var proiezione = await CreateProiezioneAsync(client, cinema1.Id, film1.Id, new DateTime(2026, 4, 1), new DateTime(2026, 4, 1, 19, 0, 0));

        var request = new ProiezioneUpdateDTO
        {
            CinemaId = cinema2.Id,
            FilmId = film2.Id,
            Data = new DateTime(2026, 4, 2),
            Ora = new DateTime(2026, 4, 2, 21, 30, 0)
        };

        var response = await client.PutAsJsonAsync($"/proiezioni/{proiezione.Id}", request);

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<ProiezioneDTO>();
        Assert.NotNull(payload);
        Assert.Equal(cinema2.Id, payload.CinemaId);
        Assert.Equal(film2.Id, payload.FilmId);
    }

    [Fact]
    public async Task P8_DeleteProiezioni_DeletesEntity_WhenExists()
    {
        await _factory.ResetDatabaseAsync();
        var client = _factory.CreateAdminClient();
        var regista = await CreateRegistaAsync(client, "Luca", "Guadagnino", "IT");
        var film = await CreateFilmAsync(client, regista.Id, "Challengers");
        var cinema = await CreateCinemaAsync(client, "Cinema Test", "Via Test", "Firenze");
        var proiezione = await CreateProiezioneAsync(client, cinema.Id, film.Id, new DateTime(2026, 5, 1), new DateTime(2026, 5, 1, 20, 0, 0));

        var deleteResponse = await client.DeleteAsync($"/proiezioni/{proiezione.Id}");
        var getResponse = await client.GetAsync($"/proiezioni/{proiezione.Id}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task P9_GetProiezioni_WithPaginationAndSearch_ReturnsPagedResult()
    {
        await _factory.ResetDatabaseAsync();
        var client = _factory.CreateAdminClient();

        var regista = await CreateRegistaAsync(client, "Quentin", "Tarantino", "US");
        var film1 = await CreateFilmAsync(client, regista.Id, "Kill Bill Volume 1");
        var film2 = await CreateFilmAsync(client, regista.Id, "Kill Bill Volume 2");
        var cinema = await CreateCinemaAsync(client, "Cinema Centrale", "Via Roma 1", "Roma");

        _ = await CreateProiezioneAsync(client, cinema.Id, film1.Id, new DateTime(2026, 8, 1), new DateTime(2026, 8, 1, 18, 0, 0));
        _ = await CreateProiezioneAsync(client, cinema.Id, film2.Id, new DateTime(2026, 8, 2), new DateTime(2026, 8, 2, 20, 0, 0));

        var response = await client.GetAsync("/proiezioni?page=1&pageSize=1&search=Kill");

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<ProiezionePagedResultDTO>();
        Assert.NotNull(payload);
        Assert.Equal(1, payload.Page);
        Assert.Equal(1, payload.PageSize);
        Assert.Equal(2, payload.TotalCount);
        Assert.Equal(2, payload.TotalPages);
        Assert.Single(payload.Items);
    }

    [Fact]
    public async Task P10_GetProiezioni_WithoutPaginationParams_ReturnsLegacyArrayPayload()
    {
        await _factory.ResetDatabaseAsync();
        var client = _factory.CreateAdminClient();

        var regista = await CreateRegistaAsync(client, "Legacy", "Director", "IT");
        var film = await CreateFilmAsync(client, regista.Id, "Legacy Film");
        var cinema = await CreateCinemaAsync(client, "Legacy Cinema", "Via Uno", "Roma");
        _ = await CreateProiezioneAsync(client, cinema.Id, film.Id, new DateTime(2026, 9, 1), new DateTime(2026, 9, 1, 21, 0, 0));

        var response = await client.GetAsync("/proiezioni");

        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(content);
        Assert.Equal(JsonValueKind.Array, json.RootElement.ValueKind);

        var payload = JsonSerializer.Deserialize<List<ProiezioneDTO>>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        Assert.NotNull(payload);
        Assert.Single(payload);
    }

    [Fact]
    public async Task E1_DeleteRegista_WithRelatedFilm_IsHandledByConfiguredFkBehavior()
    {
        await _factory.ResetDatabaseAsync();
        var client = _factory.CreatePowerUserClient();
        var regista = await CreateRegistaAsync(client, "Regista", "Relazione", "IT");
        _ = await CreateFilmAsync(client, regista.Id, "Film Collegato");

        var deleteResponse = await client.DeleteAsync($"/registi/{regista.Id}");
        var filmsResponse = await client.GetAsync("/films/");
        var films = await filmsResponse.Content.ReadFromJsonAsync<List<FilmDTO>>();

        Assert.Equal(HttpStatusCode.InternalServerError, deleteResponse.StatusCode);
        Assert.NotNull(films);
        Assert.Single(films);
    }

    [Fact]
    public async Task E2_DeleteFilm_WithRelatedProiezione_IsHandledByConfiguredFkBehavior()
    {
        await _factory.ResetDatabaseAsync();
        var client = _factory.CreateAdminClient();
        var regista = await CreateRegistaAsync(client, "Regista", "Fk", "IT");
        var film = await CreateFilmAsync(client, regista.Id, "Film FK");
        var cinema = await CreateCinemaAsync(client, "Cinema FK", "Via FK", "Roma");
        _ = await CreateProiezioneAsync(client, cinema.Id, film.Id, new DateTime(2026, 6, 1), new DateTime(2026, 6, 1, 20, 0, 0));

        var deleteResponse = await client.DeleteAsync($"/films/{film.Id}");
        var proiezioniResponse = await client.GetAsync("/proiezioni/");
        var proiezioni = await proiezioniResponse.Content.ReadFromJsonAsync<List<ProiezioneDTO>>();

        Assert.Equal(HttpStatusCode.InternalServerError, deleteResponse.StatusCode);
        Assert.NotNull(proiezioni);
        Assert.Single(proiezioni);
    }

    [Fact]
    public async Task E3_FullCrudFlow_CreatesReadsAndDeletesInOrder()
    {
        await _factory.ResetDatabaseAsync();
        var client = _factory.CreateAdminClient();

        var regista = await CreateRegistaAsync(client, "David", "Fincher", "US");
        var film = await CreateFilmAsync(client, regista.Id, "Seven");
        var cinema = await CreateCinemaAsync(client, "Cinema Centrale", "Via Centro 7", "Napoli");
        var proiezione = await CreateProiezioneAsync(client, cinema.Id, film.Id, new DateTime(2026, 7, 7), new DateTime(2026, 7, 7, 22, 0, 0));

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/registi/{regista.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/films/{film.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/cinemas/{cinema.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/proiezioni/{proiezione.Id}")).StatusCode);

        var deleteProiezioneResponse = await client.DeleteAsync($"/proiezioni/{proiezione.Id}");
        Assert.True(deleteProiezioneResponse.IsSuccessStatusCode, $"Proiezione delete failed: {deleteProiezioneResponse.StatusCode}");

        var deleteFilmResponse = await client.DeleteAsync($"/films/{film.Id}");
        Assert.True(deleteFilmResponse.IsSuccessStatusCode, $"Film delete failed: {deleteFilmResponse.StatusCode}");

        var saleResponse = await client.GetAsync($"/cinemas/{cinema.Id}/sale");
        var sale = await saleResponse.Content.ReadFromJsonAsync<List<SalaDTO>>();
        if (sale is not null && sale.Count > 0)
        {
            await client.DeleteAsync($"/sale/{sale[0].Id}");
        }

        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/cinemas/{cinema.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/registi/{regista.Id}")).StatusCode);

        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/proiezioni/{proiezione.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/films/{film.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/cinemas/{cinema.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/registi/{regista.Id}")).StatusCode);
    }

    [Fact]
    public async Task M1_UploadCover_ReturnsOk_WithValidImage()
    {
        await _factory.ResetDatabaseAsync();
        var client = _factory.CreatePowerUserClient();

        using var content = new MultipartFormDataContent();
        var imageContent = new ByteArrayContent(CreateTestJpeg());
        imageContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");
        content.Add(imageContent, "file", "test.jpg");

        var response = await client.PostAsync("/media/covers", content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<MediaUploadResultDTO>();
        Assert.NotNull(result);
        Assert.StartsWith("/media/covers/", result.Path);
        Assert.EndsWith(".jpg", result.FileName);
        Assert.Equal("image/jpeg", result.ContentType);
    }

    [Fact]
    public async Task M2_UploadCover_ReturnsBadRequest_WhenNoFile()
    {
        await _factory.ResetDatabaseAsync();
        var client = _factory.CreatePowerUserClient();

        using var content = new MultipartFormDataContent();
        var response = await client.PostAsync("/media/covers", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task M3_UploadCover_ReturnsBadRequest_WhenUnsupportedMimeType()
    {
        await _factory.ResetDatabaseAsync();
        var client = _factory.CreatePowerUserClient();

        using var content = new MultipartFormDataContent();
        var imageContent = new ByteArrayContent(CreateTestGif());
        imageContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/gif");
        content.Add(imageContent, "file", "test.gif");

        var response = await client.PostAsync("/media/covers", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task F9_PostFilms_ReturnsBadRequest_WhenFilmatoPathIsInvalidUrl()
    {
        await _factory.ResetDatabaseAsync();
        var client = _factory.CreatePowerUserClient();
        var regista = await CreateRegistaAsync(client, "Test", "Director", "US");

        var request = new FilmCreateDTO
        {
            Titolo = "Test Film",
            DataProduzione = new DateTime(2024, 1, 1),
            RegistaId = regista.Id,
            Durata = 120,
            FilmatoPath = "not-a-valid-url"
        };

        var response = await client.PostAsJsonAsync("/films/", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task F10_PostFilms_AcceptsValidFilmatoUrl()
    {
        await _factory.ResetDatabaseAsync();
        var client = _factory.CreatePowerUserClient();
        var regista = await CreateRegistaAsync(client, "Test", "Director", "US");

        var request = new FilmCreateDTO
        {
            Titolo = "Test Film",
            DataProduzione = new DateTime(2024, 1, 1),
            RegistaId = regista.Id,
            Durata = 120,
            FilmatoPath = "https://youtube.com/watch?v=test"
        };

        var response = await client.PostAsJsonAsync("/films/", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<FilmDTO>();
        Assert.NotNull(payload);
        Assert.Equal("https://youtube.com/watch?v=test", payload.FilmatoPath);
    }

    private static byte[] CreateTestJpeg()
    {
        return new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01, 0x01, 0x00, 0x00, 0x01, 0x00, 0x01, 0x00, 0x00, 0xFF, 0xD9 };
    }

    private static byte[] CreateTestGif()
    {
        return new byte[] { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61, 0x01, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x21, 0xF9, 0x04, 0x01, 0x00, 0x00, 0x00, 0x00, 0x2C, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x01, 0x00, 0x00, 0x02, 0x01, 0x44, 0x00, 0x3B };
    }

    private static async Task<RegistaDTO> CreateRegistaAsync(HttpClient client, string nome, string cognome, string nazionalita)
    {
        var request = new RegistaCreateDTO
        {
            Nome = nome,
            Cognome = cognome,
            Nazionalita = nazionalita
        };

        var response = await client.PostAsJsonAsync("/registi/", request);
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<RegistaDTO>();
        Assert.NotNull(payload);
        return payload;
    }

    private static async Task<FilmDTO> CreateFilmAsync(HttpClient client, int registaId, string titolo)
    {
        var request = new FilmCreateDTO
        {
            Titolo = titolo,
            DataProduzione = new DateTime(2020, 1, 1),
            RegistaId = registaId,
            Durata = 120,
            CopertinaPath = "/media/default.jpg"
        };

        var response = await client.PostAsJsonAsync("/films/", request);
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<FilmDTO>();
        Assert.NotNull(payload);
        return payload;
    }

    private static async Task<CinemaDTO> CreateCinemaAsync(HttpClient client, string nome, string indirizzo, string citta)
    {
        var request = new CinemaCreateDTO
        {
            Nome = nome,
            Indirizzo = indirizzo,
            Citta = citta
        };

        var response = await client.PostAsJsonAsync("/cinemas/", request);
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<CinemaDTO>();
        Assert.NotNull(payload);
        return payload;
    }

    private static async Task<ProiezioneDTO> CreateProiezioneAsync(HttpClient client, int cinemaId, int filmId, DateTime data, DateTime ora)
    {
        var saleResponse = await client.GetAsync($"/cinemas/{cinemaId}/sale");
        List<SalaDTO>? sale = null;
        if (saleResponse.IsSuccessStatusCode)
        {
            sale = await saleResponse.Content.ReadFromJsonAsync<List<SalaDTO>>();
        }

        int salaId;
        if (sale is null || sale.Count == 0)
        {
            var salaRequest = new SalaCreateDTO
            {
                CinemaId = cinemaId,
                NumeroProgressivo = 1,
                TipoSala = TipoSala.DueD,
                Supplemento = 0
            };
            var salaResponse = await client.PostAsJsonAsync($"/cinemas/{cinemaId}/sale", salaRequest);
            salaResponse.EnsureSuccessStatusCode();
            var createdSala = await salaResponse.Content.ReadFromJsonAsync<SalaDTO>();
            salaId = createdSala!.Id;
        }
        else
        {
            salaId = sale[0].Id;
        }

        var request = new ProiezioneCreateDTO
        {
            CinemaId = cinemaId,
            FilmId = filmId,
            Data = data,
            Ora = ora
        };

        var response = await client.PostAsJsonAsync("/proiezioni/", request);
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<ProiezioneDTO>();
        Assert.NotNull(payload);
        return payload;
    }
}
