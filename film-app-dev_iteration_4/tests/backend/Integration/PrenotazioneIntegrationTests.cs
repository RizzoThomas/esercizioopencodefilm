using System.Net;
using System.Net.Http.Json;
using FilmAPI.Data;
using FilmAPI.DTO;
using FilmAPI.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FilmAPI.Tests.Integration;

public class PrenotazioneIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public PrenotazioneIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task PR1_CreatePrenotazione_ReturnsCreated_WithValidData()
    {
        await _factory.ResetDatabaseAsync(seed: async db =>
        {
            db.Users.Add(new User
            {
                Email = "user1@test.com",
                PasswordHash = "hash",
                Nome = "User",
                Cognome = "One",
                Ruolo = UserRole.User,
                DataRegistrazione = DateTime.UtcNow
            });
            await db.SaveChangesAsync();

            var regista = new Regista { Nome = "Test", Cognome = "Director", Nazionalita = "IT" };
            db.Registi.Add(regista);
            await db.SaveChangesAsync();

            var film = new Film { Titolo = "Test Film", DataProduzione = DateTime.UtcNow, RegistaId = regista.Id, Durata = 120 };
            db.Films.Add(film);
            await db.SaveChangesAsync();

            var cinema = new Cinema { Nome = "Test Cinema", Indirizzo = "Via Test 1", Citta = "Roma" };
            db.Cinemas.Add(cinema);
            await db.SaveChangesAsync();

            var proiezione = new Proiezione { CinemaId = cinema.Id, FilmId = film.Id, Data = DateTime.UtcNow.AddDays(1), Ora = DateTime.UtcNow.AddDays(1).AddHours(20) };
            db.Proiezioni.Add(proiezione);
            await db.SaveChangesAsync();
        });

        var proiezioneId = await GetProiezioneIdAsync();
        var client = _factory.CreateUserClient(userId: 1);

        var request = new PrenotazioneCreateDTO
        {
            ProiezioneId = proiezioneId,
            NumeroPosti = 2,
            Note = "Test prenotazione"
        };

        var response = await client.PostAsJsonAsync("/prenotazioni", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<PrenotazioneDTO>();
        Assert.NotNull(payload);
        Assert.True(payload.Id > 0);
        Assert.Equal(2, payload.NumeroPosti);
        Assert.Equal("Test prenotazione", payload.Note);
    }

    [Fact]
    public async Task PR2_User_SeesOnlyOwnPrenotazioni()
    {
        await _factory.ResetDatabaseAsync(seed: async db =>
        {
            var user1 = new User { Email = "user1@test.com", PasswordHash = "hash", Nome = "User", Cognome = "One", Ruolo = UserRole.User, DataRegistrazione = DateTime.UtcNow };
            var user2 = new User { Email = "user2@test.com", PasswordHash = "hash", Nome = "User", Cognome = "Two", Ruolo = UserRole.User, DataRegistrazione = DateTime.UtcNow };
            db.Users.AddRange(user1, user2);
            await db.SaveChangesAsync();

            var regista = new Regista { Nome = "Test", Cognome = "Director", Nazionalita = "IT" };
            db.Registi.Add(regista);
            await db.SaveChangesAsync();

            var film = new Film { Titolo = "Test Film", DataProduzione = DateTime.UtcNow, RegistaId = regista.Id, Durata = 120 };
            db.Films.Add(film);
            await db.SaveChangesAsync();

            var cinema = new Cinema { Nome = "Test Cinema", Indirizzo = "Via Test 1", Citta = "Roma" };
            db.Cinemas.Add(cinema);
            await db.SaveChangesAsync();

            var proiezione = new Proiezione { CinemaId = cinema.Id, FilmId = film.Id, Data = DateTime.UtcNow.AddDays(1), Ora = DateTime.UtcNow.AddDays(1).AddHours(20) };
            db.Proiezioni.Add(proiezione);
            await db.SaveChangesAsync();

            db.Prenotazioni.Add(new Prenotazione { UserId = user1.Id, ProiezioneId = proiezione.Id, NumeroPosti = 1, DataPrenotazione = DateTime.UtcNow });
            db.Prenotazioni.Add(new Prenotazione { UserId = user2.Id, ProiezioneId = proiezione.Id, NumeroPosti = 2, DataPrenotazione = DateTime.UtcNow });
            await db.SaveChangesAsync();
        });

        var client1 = _factory.CreateUserClient(userId: 1);
        var response1 = await client1.GetAsync("/prenotazioni");
        var payload1 = await response1.Content.ReadFromJsonAsync<List<PrenotazioneDTO>>();
        Assert.NotNull(payload1);
        Assert.Single(payload1);

        var client2 = _factory.CreateUserClient(userId: 2);
        var response2 = await client2.GetAsync("/prenotazioni");
        var payload2 = await response2.Content.ReadFromJsonAsync<List<PrenotazioneDTO>>();
        Assert.NotNull(payload2);
        Assert.Single(payload2);
    }

    [Fact]
    public async Task PR3_User_CannotDeleteAnotherUsersPrenotazione()
    {
        await _factory.ResetDatabaseAsync(seed: async db =>
        {
            var user1 = new User { Email = "user1@test.com", PasswordHash = "hash", Nome = "User", Cognome = "One", Ruolo = UserRole.User, DataRegistrazione = DateTime.UtcNow };
            var user2 = new User { Email = "user2@test.com", PasswordHash = "hash", Nome = "User", Cognome = "Two", Ruolo = UserRole.User, DataRegistrazione = DateTime.UtcNow };
            db.Users.AddRange(user1, user2);
            await db.SaveChangesAsync();

            var regista = new Regista { Nome = "Test", Cognome = "Director", Nazionalita = "IT" };
            db.Registi.Add(regista);
            await db.SaveChangesAsync();

            var film = new Film { Titolo = "Test Film", DataProduzione = DateTime.UtcNow, RegistaId = regista.Id, Durata = 120 };
            db.Films.Add(film);
            await db.SaveChangesAsync();

            var cinema = new Cinema { Nome = "Test Cinema", Indirizzo = "Via Test 1", Citta = "Roma" };
            db.Cinemas.Add(cinema);
            await db.SaveChangesAsync();

            var proiezione = new Proiezione { CinemaId = cinema.Id, FilmId = film.Id, Data = DateTime.UtcNow.AddDays(1), Ora = DateTime.UtcNow.AddDays(1).AddHours(20) };
            db.Proiezioni.Add(proiezione);
            await db.SaveChangesAsync();

            db.Prenotazioni.Add(new Prenotazione { UserId = user1.Id, ProiezioneId = proiezione.Id, NumeroPosti = 1, DataPrenotazione = DateTime.UtcNow });
            await db.SaveChangesAsync();
        });

        var client2 = _factory.CreateUserClient(userId: 2);
        var response = await client2.DeleteAsync("/prenotazioni/1");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PR4_Admin_SeesAllPrenotazioni()
    {
        await _factory.ResetDatabaseAsync(seed: async db =>
        {
            var user1 = new User { Email = "user1@test.com", PasswordHash = "hash", Nome = "User", Cognome = "One", Ruolo = UserRole.User, DataRegistrazione = DateTime.UtcNow };
            var user2 = new User { Email = "user2@test.com", PasswordHash = "hash", Nome = "User", Cognome = "Two", Ruolo = UserRole.User, DataRegistrazione = DateTime.UtcNow };
            var admin = new User { Email = "admin@test.com", PasswordHash = "hash", Nome = "Admin", Cognome = "User", Ruolo = UserRole.Admin, DataRegistrazione = DateTime.UtcNow };
            db.Users.AddRange(user1, user2, admin);
            await db.SaveChangesAsync();

            var regista = new Regista { Nome = "Test", Cognome = "Director", Nazionalita = "IT" };
            db.Registi.Add(regista);
            await db.SaveChangesAsync();

            var film = new Film { Titolo = "Test Film", DataProduzione = DateTime.UtcNow, RegistaId = regista.Id, Durata = 120 };
            db.Films.Add(film);
            await db.SaveChangesAsync();

            var cinema = new Cinema { Nome = "Test Cinema", Indirizzo = "Via Test 1", Citta = "Roma" };
            db.Cinemas.Add(cinema);
            await db.SaveChangesAsync();

            var proiezione = new Proiezione { CinemaId = cinema.Id, FilmId = film.Id, Data = DateTime.UtcNow.AddDays(1), Ora = DateTime.UtcNow.AddDays(1).AddHours(20) };
            db.Proiezioni.Add(proiezione);
            await db.SaveChangesAsync();

            db.Prenotazioni.Add(new Prenotazione { UserId = user1.Id, ProiezioneId = proiezione.Id, NumeroPosti = 1, DataPrenotazione = DateTime.UtcNow });
            db.Prenotazioni.Add(new Prenotazione { UserId = user2.Id, ProiezioneId = proiezione.Id, NumeroPosti = 2, DataPrenotazione = DateTime.UtcNow });
            await db.SaveChangesAsync();
        });

        var adminClient = _factory.CreateAdminClient(userId: 3);
        var response = await adminClient.GetAsync("/prenotazioni");
        var payload = await response.Content.ReadFromJsonAsync<List<PrenotazioneDTO>>();

        Assert.NotNull(payload);
        Assert.Equal(2, payload.Count);
    }

    [Fact]
    public async Task PR5_DeletePrenotazione_ReturnsNotFound_WhenNonExistent()
    {
        await _factory.ResetDatabaseAsync(seed: async db =>
        {
            db.Users.Add(new User
            {
                Email = "user1@test.com",
                PasswordHash = "hash",
                Nome = "User",
                Cognome = "One",
                Ruolo = UserRole.User,
                DataRegistrazione = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        });

        var client = _factory.CreateUserClient(userId: 1);
        var response = await client.DeleteAsync("/prenotazioni/99999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<int> GetProiezioneIdAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FilmDbContext>();
        var proiezione = await db.Proiezioni.FirstOrDefaultAsync();
        return proiezione?.Id ?? 0;
    }
}
