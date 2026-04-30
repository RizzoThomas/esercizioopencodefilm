using System.Net;
using System.Net.Http.Json;
using FilmAPI.DTO;
using FilmAPI.Model;
using Microsoft.EntityFrameworkCore;

namespace FilmAPI.Tests.Integration;

public class CheckoutIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public CheckoutIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CH1_GetSeatMap_ReturnsSeatMapWithAvailableSeats()
    {
        await _factory.ResetDatabaseAsync(db => SeedCinemaSalaFilmShowWithPostiAsync(db));
        var client = _factory.CreateUserClient(1);

        var response = await client.GetAsync("/checkout/shows/1/seat-map");

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<SeatMapDTO>();
        Assert.NotNull(payload);
        Assert.Equal(1, payload.ShowId);
        Assert.Equal("Film Test", payload.FilmTitolo);
        Assert.Equal("Cinema Test", payload.CinemaNome);
        Assert.Equal("Sala 1", payload.SalaNome);
        Assert.Equal(5, payload.Posti.Count);
        Assert.All(payload.Posti, p => Assert.Equal(SeatStatus.Available, p.Stato));
    }

    [Fact]
    public async Task CH2_GetSeatMap_ShowNotFound_ReturnsNotFound()
    {
        await _factory.ResetDatabaseAsync();
        var client = _factory.CreateUserClient(1);

        var response = await client.GetAsync("/checkout/shows/999/seat-map");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CH3_CreateHold_ReturnsHoldToken()
    {
        await _factory.ResetDatabaseAsync(db => SeedCinemaSalaFilmShowWithPostiAsync(db));
        var client = _factory.CreateUserClient(1);

        var request = new SeatHoldRequestDTO
        {
            ShowId = 1,
            SalaPostoIds = new List<int> { 1, 2 }
        };

        var response = await client.PostAsJsonAsync("/checkout/holds", request);

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<SeatHoldResponseDTO>();
        Assert.NotNull(payload);
        Assert.NotEmpty(payload.HoldToken);
        Assert.Equal(2, payload.SalaPostoIds.Count);
        Assert.Empty(payload.Conflitti);
    }

    [Fact]
    public async Task CH4_CreateHold_ExceedsMaxSeats_ReturnsBadRequest()
    {
        await _factory.ResetDatabaseAsync(db => SeedCinemaSalaFilmShowWithPostiAsync(db, seatCount: 15));
        var client = _factory.CreateUserClient(1);

        var request = new SeatHoldRequestDTO
        {
            ShowId = 1,
            SalaPostoIds = Enumerable.Range(1, 11).ToList()
        };

        var response = await client.PostAsJsonAsync("/checkout/holds", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CH5_CreateHold_ConflictOnAlreadyHeldByOther_ReturnsConflict()
    {
        await _factory.ResetDatabaseAsync(db => SeedCinemaSalaFilmShowWithPostiAsync(db));
        var client1 = _factory.CreateUserClient(1);
        var client2 = _factory.CreateUserClient(2);

        var hold1 = new SeatHoldRequestDTO
        {
            ShowId = 1,
            SalaPostoIds = new List<int> { 1 }
        };

        var response1 = await client1.PostAsJsonAsync("/checkout/holds", hold1);
        response1.EnsureSuccessStatusCode();

        var hold2 = new SeatHoldRequestDTO
        {
            ShowId = 1,
            SalaPostoIds = new List<int> { 1 }
        };

        var response2 = await client2.PostAsJsonAsync("/checkout/holds", hold2);

        Assert.Equal(HttpStatusCode.Conflict, response2.StatusCode);
        var payload = await response2.Content.ReadFromJsonAsync<SeatHoldResponseDTO>();
        Assert.NotNull(payload);
        Assert.NotEmpty(payload.Conflitti);
    }

    [Fact]
    public async Task CH6_CreateHold_SameUserCanExtendHold()
    {
        await _factory.ResetDatabaseAsync(db => SeedCinemaSalaFilmShowWithPostiAsync(db));
        var client = _factory.CreateUserClient(1);

        var hold1 = new SeatHoldRequestDTO
        {
            ShowId = 1,
            SalaPostoIds = new List<int> { 1 }
        };

        var response1 = await client.PostAsJsonAsync("/checkout/holds", hold1);
        response1.EnsureSuccessStatusCode();

        var hold2 = new SeatHoldRequestDTO
        {
            ShowId = 1,
            SalaPostoIds = new List<int> { 1, 2 }
        };

        var response2 = await client.PostAsJsonAsync("/checkout/holds", hold2);
        response2.EnsureSuccessStatusCode();

        var payload = await response2.Content.ReadFromJsonAsync<SeatHoldResponseDTO>();
        Assert.NotNull(payload);
        Assert.Equal(2, payload.SalaPostoIds.Count);
    }

    [Fact]
    public async Task CH7_RefreshHold_ExtendsExpiration()
    {
        await _factory.ResetDatabaseAsync(db => SeedCinemaSalaFilmShowWithPostiAsync(db));
        var client = _factory.CreateUserClient(1);

        var holdRequest = new SeatHoldRequestDTO
        {
            ShowId = 1,
            SalaPostoIds = new List<int> { 1 }
        };

        var holdResponse = await client.PostAsJsonAsync("/checkout/holds", holdRequest);
        holdResponse.EnsureSuccessStatusCode();
        var holdPayload = await holdResponse.Content.ReadFromJsonAsync<SeatHoldResponseDTO>();
        Assert.NotNull(holdPayload);

        var response = await client.PostAsync($"/checkout/holds/{holdPayload.HoldToken}/refresh", null);

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<SeatHoldResponseDTO>();
        Assert.NotNull(payload);
        Assert.Equal(holdPayload.HoldToken, payload.HoldToken);
        Assert.True(payload.ScadeAtUtc > holdPayload.ScadeAtUtc);
    }

    [Fact]
    public async Task CH8_ReleaseHold_RemovesHold()
    {
        await _factory.ResetDatabaseAsync(db => SeedCinemaSalaFilmShowWithPostiAsync(db));
        var client = _factory.CreateUserClient(1);

        var holdRequest = new SeatHoldRequestDTO
        {
            ShowId = 1,
            SalaPostoIds = new List<int> { 1 }
        };

        var holdResponse = await client.PostAsJsonAsync("/checkout/holds", holdRequest);
        holdResponse.EnsureSuccessStatusCode();
        var holdPayload = await holdResponse.Content.ReadFromJsonAsync<SeatHoldResponseDTO>();
        Assert.NotNull(holdPayload);

        var response = await client.DeleteAsync($"/checkout/holds/{holdPayload.HoldToken}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var seatMapResponse = await client.GetAsync("/checkout/shows/1/seat-map");
        seatMapResponse.EnsureSuccessStatusCode();
        var seatMap = await seatMapResponse.Content.ReadFromJsonAsync<SeatMapDTO>();
        Assert.NotNull(seatMap);
        var posto1 = seatMap.Posti.First(p => p.SalaPostoId == 1);
        Assert.Equal(SeatStatus.Available, posto1.Stato);
    }

    [Fact]
    public async Task CH9_CreateOrdine_FromValidHold_CreatesPendingOrder()
    {
        await _factory.ResetDatabaseAsync(db => SeedCinemaSalaFilmShowWithPostiAsync(db));
        var client = _factory.CreateUserClient(1);

        var holdRequest = new SeatHoldRequestDTO
        {
            ShowId = 1,
            SalaPostoIds = new List<int> { 1, 2 }
        };

        var holdResponse = await client.PostAsJsonAsync("/checkout/holds", holdRequest);
        holdResponse.EnsureSuccessStatusCode();
        var holdPayload = await holdResponse.Content.ReadFromJsonAsync<SeatHoldResponseDTO>();
        Assert.NotNull(holdPayload);

        var ordineRequest = new CreateOrdineRequestDTO
        {
            HoldToken = holdPayload.HoldToken
        };

        var response = await client.PostAsJsonAsync("/checkout/orders", ordineRequest);

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<OrdineSummaryDTO>();
        Assert.NotNull(payload);
        Assert.Equal(2, payload.NumeroBiglietti);
        Assert.Equal("Pending", payload.Stato);
        Assert.Equal(20m, payload.TotaleLordo);
    }

    [Fact]
    public async Task CH10_CreateOrderIdempotent_SameHoldToken_ReturnsExistingOrder()
    {
        await _factory.ResetDatabaseAsync(db => SeedCinemaSalaFilmShowWithPostiAsync(db));
        var client = _factory.CreateUserClient(1);

        var holdRequest = new SeatHoldRequestDTO
        {
            ShowId = 1,
            SalaPostoIds = new List<int> { 1 }
        };

        var holdResponse = await client.PostAsJsonAsync("/checkout/holds", holdRequest);
        holdResponse.EnsureSuccessStatusCode();
        var holdPayload = await holdResponse.Content.ReadFromJsonAsync<SeatHoldResponseDTO>();
        Assert.NotNull(holdPayload);

        var ordineRequest = new CreateOrdineRequestDTO
        {
            HoldToken = holdPayload.HoldToken
        };

        var response1 = await client.PostAsJsonAsync("/checkout/orders", ordineRequest);
        response1.EnsureSuccessStatusCode();
        var payload1 = await response1.Content.ReadFromJsonAsync<OrdineSummaryDTO>();
        Assert.NotNull(payload1);

        var response2 = await client.PostAsJsonAsync("/checkout/orders", ordineRequest);
        response2.EnsureSuccessStatusCode();
        var payload2 = await response2.Content.ReadFromJsonAsync<OrdineSummaryDTO>();
        Assert.NotNull(payload2);

        Assert.Equal(payload1.Id, payload2.Id);
    }

    [Fact]
    public async Task CH11_GetOrdiniByUser_ReturnsUserOrders()
    {
        await _factory.ResetDatabaseAsync(db => SeedCinemaSalaFilmShowWithPostiAsync(db));
        var client = _factory.CreateUserClient(1);

        var holdRequest = new SeatHoldRequestDTO
        {
            ShowId = 1,
            SalaPostoIds = new List<int> { 1 }
        };

        var holdResponse = await client.PostAsJsonAsync("/checkout/holds", holdRequest);
        holdResponse.EnsureSuccessStatusCode();
        var holdPayload = await holdResponse.Content.ReadFromJsonAsync<SeatHoldResponseDTO>();
        Assert.NotNull(holdPayload);

        var ordineRequest = new CreateOrdineRequestDTO
        {
            HoldToken = holdPayload.HoldToken
        };

        await client.PostAsJsonAsync("/checkout/orders", ordineRequest);

        var response = await client.GetAsync("/checkout/orders");

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<List<OrdineSummaryDTO>>();
        Assert.NotNull(payload);
        Assert.Single(payload);
    }

    [Fact]
    public async Task CH12_GetOrdineById_WithOwnershipCheck_ReturnsOrder()
    {
        await _factory.ResetDatabaseAsync(db => SeedCinemaSalaFilmShowWithPostiAsync(db));
        var client = _factory.CreateUserClient(1);

        var holdRequest = new SeatHoldRequestDTO
        {
            ShowId = 1,
            SalaPostoIds = new List<int> { 1 }
        };

        var holdResponse = await client.PostAsJsonAsync("/checkout/holds", holdRequest);
        holdResponse.EnsureSuccessStatusCode();
        var holdPayload = await holdResponse.Content.ReadFromJsonAsync<SeatHoldResponseDTO>();
        Assert.NotNull(holdPayload);

        var ordineRequest = new CreateOrdineRequestDTO
        {
            HoldToken = holdPayload.HoldToken
        };

        var ordineResponse = await client.PostAsJsonAsync("/checkout/orders", ordineRequest);
        ordineResponse.EnsureSuccessStatusCode();
        var ordinePayload = await ordineResponse.Content.ReadFromJsonAsync<OrdineSummaryDTO>();
        Assert.NotNull(ordinePayload);

        var response = await client.GetAsync($"/checkout/orders/{ordinePayload.Id}");

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<OrdineSummaryDTO>();
        Assert.NotNull(payload);
        Assert.Equal(ordinePayload.Id, payload.Id);
    }

    [Fact]
    public async Task CH13_GetOrdineById_OtherUser_ReturnsNotFound()
    {
        await _factory.ResetDatabaseAsync(db => SeedCinemaSalaFilmShowWithPostiAsync(db));
        var client1 = _factory.CreateUserClient(1);
        var client2 = _factory.CreateUserClient(2);

        var holdRequest = new SeatHoldRequestDTO
        {
            ShowId = 1,
            SalaPostoIds = new List<int> { 1 }
        };

        var holdResponse = await client1.PostAsJsonAsync("/checkout/holds", holdRequest);
        holdResponse.EnsureSuccessStatusCode();
        var holdPayload = await holdResponse.Content.ReadFromJsonAsync<SeatHoldResponseDTO>();
        Assert.NotNull(holdPayload);

        var ordineRequest = new CreateOrdineRequestDTO
        {
            HoldToken = holdPayload.HoldToken
        };

        var ordineResponse = await client1.PostAsJsonAsync("/checkout/orders", ordineRequest);
        ordineResponse.EnsureSuccessStatusCode();
        var ordinePayload = await ordineResponse.Content.ReadFromJsonAsync<OrdineSummaryDTO>();
        Assert.NotNull(ordinePayload);

        var response = await client2.GetAsync($"/checkout/orders/{ordinePayload.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CH14_SeatMap_ShowsHeldByMe_AfterHold()
    {
        await _factory.ResetDatabaseAsync(db => SeedCinemaSalaFilmShowWithPostiAsync(db));
        var client = _factory.CreateUserClient(1);

        var holdRequest = new SeatHoldRequestDTO
        {
            ShowId = 1,
            SalaPostoIds = new List<int> { 1 }
        };

        await client.PostAsJsonAsync("/checkout/holds", holdRequest);

        var response = await client.GetAsync("/checkout/shows/1/seat-map");
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<SeatMapDTO>();
        Assert.NotNull(payload);
        var posto1 = payload.Posti.First(p => p.SalaPostoId == 1);
        Assert.Equal(SeatStatus.HeldByMe, posto1.Stato);
        Assert.NotNull(payload.ScadeAtUtc);
    }

    [Fact]
    public async Task CH15_SeatMap_ShowsHeldByOther_AfterOtherUserHold()
    {
        await _factory.ResetDatabaseAsync(db => SeedCinemaSalaFilmShowWithPostiAsync(db));
        var client1 = _factory.CreateUserClient(1);
        var client2 = _factory.CreateUserClient(2);

        var holdRequest = new SeatHoldRequestDTO
        {
            ShowId = 1,
            SalaPostoIds = new List<int> { 1 }
        };

        await client1.PostAsJsonAsync("/checkout/holds", holdRequest);

        var response = await client2.GetAsync("/checkout/shows/1/seat-map");
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<SeatMapDTO>();
        Assert.NotNull(payload);
        var posto1 = payload.Posti.First(p => p.SalaPostoId == 1);
        Assert.Equal(SeatStatus.HeldByOther, posto1.Stato);
    }

    [Fact]
    public async Task CH16_ConcurrentHoldOnSameSeat_OnlyOneSucceeds()
    {
        await _factory.ResetDatabaseAsync(db => SeedCinemaSalaFilmShowWithPostiAsync(db));
        var client1 = _factory.CreateUserClient(1);
        var client2 = _factory.CreateUserClient(2);

        var request = new SeatHoldRequestDTO
        {
            ShowId = 1,
            SalaPostoIds = new List<int> { 1 }
        };

        var task1 = client1.PostAsJsonAsync("/checkout/holds", request);
        var task2 = client2.PostAsJsonAsync("/checkout/holds", request);

        var results = await Task.WhenAll(task1, task2);

        var successCount = 0;
        if (results[0].IsSuccessStatusCode) successCount++;
        if (results[1].IsSuccessStatusCode) successCount++;

        Assert.Equal(1, successCount);
    }

    [Fact]
    public async Task CH17_ConcurrentHoldOnMultipleSeats_PartialConflictHandled()
    {
        await _factory.ResetDatabaseAsync(db => SeedCinemaSalaFilmShowWithPostiAsync(db));
        var client1 = _factory.CreateUserClient(1);
        var client2 = _factory.CreateUserClient(2);

        var request1 = new SeatHoldRequestDTO
        {
            ShowId = 1,
            SalaPostoIds = new List<int> { 1, 2 }
        };

        var request2 = new SeatHoldRequestDTO
        {
            ShowId = 1,
            SalaPostoIds = new List<int> { 2, 3 }
        };

        var task1 = client1.PostAsJsonAsync("/checkout/holds", request1);
        var task2 = client2.PostAsJsonAsync("/checkout/holds", request2);

        var results = await Task.WhenAll(task1, task2);

        var response1 = results[0];
        var response2 = results[1];

        var success1 = response1.IsSuccessStatusCode;
        var success2 = response2.IsSuccessStatusCode;

        if (success1 && success2)
        {
            var payload1 = await response1.Content.ReadFromJsonAsync<SeatHoldResponseDTO>();
            var payload2 = await response2.Content.ReadFromJsonAsync<SeatHoldResponseDTO>();
            Assert.NotNull(payload1);
            Assert.NotNull(payload2);
            var commonSeats = payload1.SalaPostoIds.Intersect(payload2.SalaPostoIds).ToList();
            Assert.Empty(commonSeats);
        }
        else
        {
            Assert.True(!success1 || !success2);
        }
    }

    [Fact]
    public async Task CH18_CreateOrdineWithIdempotencyKey_PreventsDuplicate()
    {
        await _factory.ResetDatabaseAsync(db => SeedCinemaSalaFilmShowWithPostiAsync(db));
        var client = _factory.CreateUserClient(1);

        var holdRequest = new SeatHoldRequestDTO
        {
            ShowId = 1,
            SalaPostoIds = new List<int> { 1 }
        };

        var holdResponse = await client.PostAsJsonAsync("/checkout/holds", holdRequest);
        holdResponse.EnsureSuccessStatusCode();
        var holdPayload = await holdResponse.Content.ReadFromJsonAsync<SeatHoldResponseDTO>();
        Assert.NotNull(holdPayload);

        var idempotencyKey = $"idem-{Guid.NewGuid():N}";
        var ordineRequest = new CreateOrdineRequestDTO
        {
            HoldToken = holdPayload.HoldToken,
            IdempotencyKey = idempotencyKey
        };

        var response1 = await client.PostAsJsonAsync("/checkout/orders", ordineRequest);
        response1.EnsureSuccessStatusCode();
        var payload1 = await response1.Content.ReadFromJsonAsync<OrdineSummaryDTO>();
        Assert.NotNull(payload1);

        var response2 = await client.PostAsJsonAsync("/checkout/orders", ordineRequest);
        response2.EnsureSuccessStatusCode();
        var payload2 = await response2.Content.ReadFromJsonAsync<OrdineSummaryDTO>();
        Assert.NotNull(payload2);

        Assert.Equal(payload1.Id, payload2.Id);
    }

    [Fact]
    public async Task CH19_CreateOrdine_EmptyHoldToken_ReturnsBadRequest()
    {
        await _factory.ResetDatabaseAsync(db => SeedCinemaSalaFilmShowWithPostiAsync(db));
        var client = _factory.CreateUserClient(1);

        var request = new CreateOrdineRequestDTO
        {
            HoldToken = ""
        };

        var response = await client.PostAsJsonAsync("/checkout/orders", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CH20_CreateOrdine_InvalidHoldToken_ReturnsConflict()
    {
        await _factory.ResetDatabaseAsync(db => SeedCinemaSalaFilmShowWithPostiAsync(db));
        var client = _factory.CreateUserClient(1);

        var request = new CreateOrdineRequestDTO
        {
            HoldToken = "invalid-token-nonexistent"
        };

        var response = await client.PostAsJsonAsync("/checkout/orders", request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    private static async Task SeedCinemaSalaFilmShowWithPostiAsync(FilmAPI.Data.FilmDbContext db, int seatCount = 5)
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

        for (int i = 1; i <= seatCount; i++)
        {
            db.SalaPosti.Add(new SalaPosto
            {
                SalaId = sala.Id,
                Settore = "PLATEA",
                Fila = 1,
                Numero = i,
                IsAttivo = true
            });
        }
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

        var user1 = new User
        {
            Email = "user1@checkout.com",
            PasswordHash = "hash",
            Nome = "User",
            Cognome = "One",
            Ruolo = UserRole.User,
            DataRegistrazione = DateTime.UtcNow,
            CreditoResiduo = 0
        };
        db.Users.Add(user1);

        var user2 = new User
        {
            Email = "user2@checkout.com",
            PasswordHash = "hash",
            Nome = "User",
            Cognome = "Two",
            Ruolo = UserRole.User,
            DataRegistrazione = DateTime.UtcNow,
            CreditoResiduo = 0
        };
        db.Users.Add(user2);
        await db.SaveChangesAsync();

        var show = new Show
        {
            CinemaId = cinema.Id,
            SalaId = sala.Id,
            FilmId = film.Id,
            StartAtUtc = DateTime.UtcNow.AddHours(1),
            DurataMinutiSnapshot = 120,
            PrezzoBase = 10,
            SupplementoSala = 0
        };
        db.Shows.Add(show);
        await db.SaveChangesAsync();
    }
}
