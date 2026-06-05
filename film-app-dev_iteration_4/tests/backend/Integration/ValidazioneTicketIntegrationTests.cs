using System.Net;
using System.Net.Http.Json;
using FilmAPI.Data;
using FilmAPI.DTO;
using FilmAPI.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FilmAPI.Tests.Integration;

/// <summary>Suite di test per ValidazioneTicketIntegrationTests.</summary>
public class ValidazioneTicketIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ValidazioneTicketIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    /// <summary>Verifica lo scenario di VT1_GetTicketValidationLookup_ReturnsTicketDetails: predispone i dati e le condizioni previste dal caso di test e controlla che l'esito atteso venga restituito.</summary>
    [Fact]
    public async Task VT1_GetTicketValidationLookup_ReturnsTicketDetails()
    {
        await _factory.ResetDatabaseAsync(db => SeedValidatedScenarioAsync(db));
        var client = _factory.CreatePowerUserClient(10);

        var response = await client.GetAsync("/admin/tickets/validate/CB-20260418-AAAA1111");

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<TicketValidationLookupDTO>();
        Assert.NotNull(payload);
        Assert.Equal("Film Test", payload.FilmTitolo);
        Assert.Equal("Cinema Roma Centro", payload.CinemaNome);
        Assert.Equal("ROMA-001", payload.CinemaCodiceLocale);
        Assert.Equal("Issued", payload.Stato);
    }

    /// <summary>Verifica lo scenario di VT2_ValidateTicket_Twice_SecondCallReturnsConflict: predispone i dati e le condizioni previste dal caso di test e controlla che l'esito atteso venga restituito.</summary>
    [Fact]
    public async Task VT2_ValidateTicket_Twice_SecondCallReturnsConflict()
    {
        await _factory.ResetDatabaseAsync(db => SeedValidatedScenarioAsync(db));
        var client = _factory.CreatePowerUserClient(10);

        var firstResponse = await client.PostAsJsonAsync("/admin/tickets/validate", new TicketValidationRequestDTO
        {
            CodiceBiglietto = "CB-20260418-AAAA1111",
            CinemaId = 1
        });

        firstResponse.EnsureSuccessStatusCode();
        var firstPayload = await firstResponse.Content.ReadFromJsonAsync<TicketValidationResultDTO>();
        Assert.NotNull(firstPayload);
        Assert.True(firstPayload.Success);
        Assert.Equal("Validated", firstPayload.Ticket.Stato);
        Assert.NotNull(firstPayload.Ticket.ValidatoAtUtc);
        Assert.Equal(10, firstPayload.Ticket.ValidatoDaUserId);
        Assert.Equal(1, firstPayload.Ticket.ValidatoCinemaId);

        var secondResponse = await client.PostAsJsonAsync("/admin/tickets/validate", new TicketValidationRequestDTO
        {
            CodiceBiglietto = "CB-20260418-AAAA1111",
            CinemaId = 1
        });

        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FilmDbContext>();
        var ticket = await db.Biglietti.SingleAsync(t => t.CodiceBiglietto == "CB-20260418-AAAA1111");
        Assert.Equal(BigliettoState.Validated, ticket.Stato);
        Assert.Equal(10, ticket.ValidatoDaUserId);
        Assert.Equal(1, ticket.ValidatoCinemaId);
        Assert.NotNull(ticket.ValidatoAtUtc);
    }

    /// <summary>Verifica lo scenario di VT3_ValidateTicket_WithCinemaMismatch_ReturnsConflict: predispone i dati e le condizioni previste dal caso di test e controlla che l'esito atteso venga restituito.</summary>
    [Fact]
    public async Task VT3_ValidateTicket_WithCinemaMismatch_ReturnsConflict()
    {
        await _factory.ResetDatabaseAsync(db => SeedValidatedScenarioAsync(db));
        var client = _factory.CreatePowerUserClient(10);

        var response = await client.PostAsJsonAsync("/admin/tickets/validate", new TicketValidationRequestDTO
        {
            CodiceBiglietto = "CB-20260418-AAAA1111",
            CinemaId = 2
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FilmDbContext>();
        var ticket = await db.Biglietti.SingleAsync(t => t.CodiceBiglietto == "CB-20260418-AAAA1111");
        Assert.Equal(BigliettoState.Issued, ticket.Stato);
        Assert.Null(ticket.ValidatoAtUtc);
        Assert.Null(ticket.ValidatoDaUserId);
        Assert.Null(ticket.ValidatoCinemaId);
    }

    private static async Task SeedValidatedScenarioAsync(FilmDbContext db)
    {
        var cinema1 = new Cinema
        {
            Id = 1,
            Nome = "Cinema Roma Centro",
            Citta = "Roma",
            Indirizzo = "Via Roma 1",
            CodiceLocale = "ROMA-001"
        };
        var cinema2 = new Cinema
        {
            Id = 2,
            Nome = "Cinema Milano Nord",
            Citta = "Milano",
            Indirizzo = "Via Milano 2",
            CodiceLocale = "MILANO-002"
        };
        db.Cinemas.AddRange(cinema1, cinema2);

        var sala = new Sala
        {
            Id = 1,
            CinemaId = 1,
            NumeroProgressivo = 1,
            TipoSala = TipoSala.DueD,
            Nome = "Sala 1",
            Supplemento = 0m,
            IsAttiva = true
        };
        db.Sale.Add(sala);

        var posto = new SalaPosto
        {
            Id = 1,
            SalaId = 1,
            Settore = "PLATEA",
            Fila = 5,
            Numero = 8,
            IsAttivo = true
        };
        db.SalaPosti.Add(posto);

        var regista = new Regista { Id = 1, Nome = "Test", Cognome = "Regista" };
        db.Registi.Add(regista);

        var film = new Film
        {
            Id = 1,
            Titolo = "Film Test",
            Durata = 120,
            RegistaId = 1,
            DataProduzione = new DateTime(2024, 1, 1)
        };
        db.Films.Add(film);

        db.Users.AddRange(
            new User
            {
                Id = 1,
                Email = "customer@cinebase.it",
                PasswordHash = "hash",
                Nome = "Customer",
                Cognome = "One",
                Ruolo = UserRole.User,
                DataRegistrazione = DateTime.UtcNow,
                CreditoResiduo = 0m
            },
            new User
            {
                Id = 10,
                Email = "poweruser@cinebase.it",
                PasswordHash = "hash",
                Nome = "Power",
                Cognome = "User",
                Ruolo = UserRole.PowerUser,
                DataRegistrazione = DateTime.UtcNow,
                CreditoResiduo = 0m
            });
        await db.SaveChangesAsync();

        var show = new Show
        {
            Id = 1,
            CinemaId = 1,
            SalaId = 1,
            FilmId = 1,
            StartAtUtc = DateTime.UtcNow.AddHours(4),
            DurataMinutiSnapshot = 120,
            PrezzoBase = 9m,
            SupplementoSala = 1m
        };
        db.Shows.Add(show);
        await db.SaveChangesAsync();

        var ordine = new Ordine
        {
            Id = 1,
            CodiceOrdine = "ORD-VALID-001",
            UserId = 1,
            ShowId = 1,
            CinemaId = 1,
            SalaId = 1,
            FilmId = 1,
            HoldToken = "hold-valid-001",
            NumeroBiglietti = 1,
            TotaleLordo = 10m,
            ImportoCredito = 0m,
            ImportoCarta = 10m,
            Stato = OrdineState.Paid,
            CreatedAtUtc = DateTime.UtcNow.AddMinutes(-30),
            PaidAtUtc = DateTime.UtcNow.AddMinutes(-20)
        };
        db.Ordini.Add(ordine);

        db.Biglietti.Add(new Biglietto
        {
            Id = 1,
            OrdineId = 1,
            ShowId = 1,
            SalaPostoId = 1,
            UserId = 1,
            CodiceBiglietto = "CB-20260418-AAAA1111",
            BarcodeValue = "CB-20260418-AAAA1111",
            PrezzoBase = 9m,
            Supplemento = 1m,
            PrezzoTotale = 10m,
            Stato = BigliettoState.Issued
        });
        await db.SaveChangesAsync();
    }
}
