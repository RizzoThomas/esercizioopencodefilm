using System.Net;
using System.Net.Http.Json;
using System.Text;
using FilmAPI.Data;
using FilmAPI.DTO;
using FilmAPI.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FilmAPI.Tests.Integration;

/// <summary>Suite di test per PagamentoCreditoIntegrationTests.</summary>
public class PagamentoCreditoIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public PagamentoCreditoIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    /// <summary>Verifica lo scenario di PG1_PayOrder_WithCard_CompletesSynchronouslyAfterPaymentIntentSucceeded: predispone i dati e le condizioni previste dal caso di test e controlla che l'esito atteso venga restituito.</summary>
    [Fact]
    public async Task PG1_PayOrder_WithCard_CompletesSynchronouslyAfterPaymentIntentSucceeded()
    {
        await _factory.ResetDatabaseAsync(db => SeedScenarioAsync(db, user1Credit: 0m));
        var client = _factory.CreateUserClient(1);

        var order = await CreatePendingOrderAsync(client, new[] { 1, 2 });

        var firstResponse = await client.PostAsJsonAsync($"/checkout/orders/{order.Id}/pay", new PayOrdineRequestDTO
        {
            MetodoPagamento = "Carta"
        });

        firstResponse.EnsureSuccessStatusCode();
        var firstPayload = await firstResponse.Content.ReadFromJsonAsync<PayOrdineResponseDTO>();
        Assert.NotNull(firstPayload);
        Assert.True(firstPayload.RequiresCardAction);
        Assert.Equal("requires_confirmation", firstPayload.StatoPagamento);
        Assert.NotNull(firstPayload.StripeClientSecret);
        Assert.NotNull(firstPayload.StripePaymentIntentId);

        _factory.StripeGateway.SetPaymentIntentStatus(firstPayload.StripePaymentIntentId!, "succeeded");

        var secondResponse = await client.PostAsJsonAsync($"/checkout/orders/{order.Id}/pay", new PayOrdineRequestDTO
        {
            MetodoPagamento = "Carta"
        });

        secondResponse.EnsureSuccessStatusCode();
        var secondPayload = await secondResponse.Content.ReadFromJsonAsync<PayOrdineResponseDTO>();
        Assert.NotNull(secondPayload);
        Assert.False(secondPayload.RequiresCardAction);
        Assert.Equal("Paid", secondPayload.StatoPagamento);
        Assert.Equal("Paid", secondPayload.Ordine.Stato);
        Assert.Equal(2, secondPayload.Ordine.Biglietti.Count);
        Assert.Equal(20m, secondPayload.Ordine.ImportoCarta);
        Assert.Equal(0m, secondPayload.Ordine.ImportoCredito);

        var ticketsResponse = await client.GetAsync("/checkout/tickets");
        ticketsResponse.EnsureSuccessStatusCode();
        var tickets = await ticketsResponse.Content.ReadFromJsonAsync<List<BigliettoSummaryDTO>>();
        Assert.NotNull(tickets);
        Assert.Equal(2, tickets.Count);
    }

    /// <summary>Verifica lo scenario di PG2_PayOrder_WithCredit_DebitsBalanceAndCreatesAuditMovement: predispone i dati e le condizioni previste dal caso di test e controlla che l'esito atteso venga restituito.</summary>
    [Fact]
    public async Task PG2_PayOrder_WithCredit_DebitsBalanceAndCreatesAuditMovement()
    {
        await _factory.ResetDatabaseAsync(db => SeedScenarioAsync(db, user1Credit: 40m));
        var client = _factory.CreateUserClient(1);

        var order = await CreatePendingOrderAsync(client, new[] { 1, 2 });

        var response = await client.PostAsJsonAsync($"/checkout/orders/{order.Id}/pay", new PayOrdineRequestDTO
        {
            MetodoPagamento = "Credito"
        });

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<PayOrdineResponseDTO>();
        Assert.NotNull(payload);
        Assert.Equal("Paid", payload.StatoPagamento);
        Assert.Equal("Paid", payload.Ordine.Stato);
        Assert.Equal(20m, payload.Ordine.ImportoCredito);
        Assert.Equal(0m, payload.Ordine.ImportoCarta);
        Assert.Equal(2, payload.Ordine.Biglietti.Count);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FilmDbContext>();
        var user = await db.Users.FirstAsync(u => u.Id == 1);
        Assert.Equal(20m, user.CreditoResiduo);

        var movement = await db.MovimentiCredito.SingleAsync(m => m.UserId == 1 && m.OrdineId == order.Id);
        Assert.Equal(MovimentoCreditoTipo.DebitOrder, movement.Tipo);
        Assert.Equal(-20m, movement.Importo);
        Assert.Equal(40m, movement.SaldoPre);
        Assert.Equal(20m, movement.SaldoPost);
    }

    /// <summary>Verifica lo scenario di PG3_PayOrder_WithMixedPayment_SplitsCreditAndCardAndFinalizesOrder: predispone i dati e le condizioni previste dal caso di test e controlla che l'esito atteso venga restituito.</summary>
    [Fact]
    public async Task PG3_PayOrder_WithMixedPayment_SplitsCreditAndCardAndFinalizesOrder()
    {
        await _factory.ResetDatabaseAsync(db => SeedScenarioAsync(db, user1Credit: 7m));
        var client = _factory.CreateUserClient(1);

        var order = await CreatePendingOrderAsync(client, new[] { 1, 2 });

        var firstResponse = await client.PostAsJsonAsync($"/checkout/orders/{order.Id}/pay", new PayOrdineRequestDTO
        {
            MetodoPagamento = "Misto",
            ImportoCreditoRichiesto = 7m
        });

        firstResponse.EnsureSuccessStatusCode();
        var firstPayload = await firstResponse.Content.ReadFromJsonAsync<PayOrdineResponseDTO>();
        Assert.NotNull(firstPayload);
        Assert.True(firstPayload.RequiresCardAction);
        Assert.Equal(7m, firstPayload.Ordine.ImportoCredito);
        Assert.Equal(13m, firstPayload.Ordine.ImportoCarta);

        _factory.StripeGateway.SetPaymentIntentStatus(firstPayload.StripePaymentIntentId!, "succeeded");

        var secondResponse = await client.PostAsJsonAsync($"/checkout/orders/{order.Id}/pay", new PayOrdineRequestDTO
        {
            MetodoPagamento = "Misto",
            ImportoCreditoRichiesto = 7m
        });

        secondResponse.EnsureSuccessStatusCode();
        var secondPayload = await secondResponse.Content.ReadFromJsonAsync<PayOrdineResponseDTO>();
        Assert.NotNull(secondPayload);
        Assert.Equal("Paid", secondPayload.StatoPagamento);
        Assert.Equal("Paid", secondPayload.Ordine.Stato);
        Assert.Equal(7m, secondPayload.Ordine.ImportoCredito);
        Assert.Equal(13m, secondPayload.Ordine.ImportoCarta);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FilmDbContext>();
        var user = await db.Users.FirstAsync(u => u.Id == 1);
        Assert.Equal(0m, user.CreditoResiduo);
    }

    /// <summary>Verifica lo scenario di PG4_PayOrder_WithInsufficientCredit_ReturnsConflict: predispone i dati e le condizioni previste dal caso di test e controlla che l'esito atteso venga restituito.</summary>
    [Fact]
    public async Task PG4_PayOrder_WithInsufficientCredit_ReturnsConflict()
    {
        await _factory.ResetDatabaseAsync(db => SeedScenarioAsync(db, user1Credit: 5m));
        var client = _factory.CreateUserClient(1);

        var order = await CreatePendingOrderAsync(client, new[] { 1, 2 });

        var response = await client.PostAsJsonAsync($"/checkout/orders/{order.Id}/pay", new PayOrdineRequestDTO
        {
            MetodoPagamento = "Credito"
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var orderResponse = await client.GetAsync($"/checkout/orders/{order.Id}");
        orderResponse.EnsureSuccessStatusCode();
        var payload = await orderResponse.Content.ReadFromJsonAsync<OrdineSummaryDTO>();
        Assert.NotNull(payload);
        Assert.Equal("Pending", payload.Stato);
        Assert.Empty(payload.Biglietti);
    }

    /// <summary>Verifica lo scenario di PG5_StripeWebhook_ReplaySafe_DoesNotDuplicateTicketsOrMovements: predispone i dati e le condizioni previste dal caso di test e controlla che l'esito atteso venga restituito.</summary>
    [Fact]
    public async Task PG5_StripeWebhook_ReplaySafe_DoesNotDuplicateTicketsOrMovements()
    {
        await _factory.ResetDatabaseAsync(db => SeedScenarioAsync(db, user1Credit: 0m));
        var client = _factory.CreateUserClient(1);

        var order = await CreatePendingOrderAsync(client, new[] { 1, 2 });

        var payResponse = await client.PostAsJsonAsync($"/checkout/orders/{order.Id}/pay", new PayOrdineRequestDTO
        {
            MetodoPagamento = "Carta"
        });

        payResponse.EnsureSuccessStatusCode();
        var payPayload = await payResponse.Content.ReadFromJsonAsync<PayOrdineResponseDTO>();
        Assert.NotNull(payPayload);

        _factory.StripeGateway.SetPaymentIntentStatus(payPayload.StripePaymentIntentId!, "succeeded");
        var webhook = _factory.StripeGateway.CreateWebhook("evt_test_replay", "payment_intent.succeeded", payPayload.StripePaymentIntentId!);

        var webhookRequest1 = new HttpRequestMessage(HttpMethod.Post, "/payments/stripe/webhook")
        {
            Content = new StringContent(webhook.Payload, Encoding.UTF8, "application/json")
        };
        webhookRequest1.Headers.Add("Stripe-Signature", webhook.Signature);

        var webhookRequest2 = new HttpRequestMessage(HttpMethod.Post, "/payments/stripe/webhook")
        {
            Content = new StringContent(webhook.Payload, Encoding.UTF8, "application/json")
        };
        webhookRequest2.Headers.Add("Stripe-Signature", webhook.Signature);

        var response1 = await client.SendAsync(webhookRequest1);
        var response2 = await client.SendAsync(webhookRequest2);

        response1.EnsureSuccessStatusCode();
        response2.EnsureSuccessStatusCode();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FilmDbContext>();
        var paidOrder = await db.Ordini.Include(o => o.Biglietti).SingleAsync(o => o.Id == order.Id);
        Assert.Equal(OrdineState.Paid, paidOrder.Stato);
        Assert.Equal(2, paidOrder.Biglietti.Count);
        Assert.Empty(await db.MovimentiCredito.Where(m => m.OrdineId == order.Id).ToListAsync());
    }

    /// <summary>Verifica lo scenario di PG6_AdminTopUp_UpdatesBalanceAndCreditoMeHistory: predispone i dati e le condizioni previste dal caso di test e controlla che l'esito atteso venga restituito.</summary>
    [Fact]
    public async Task PG6_AdminTopUp_UpdatesBalanceAndCreditoMeHistory()
    {
        await _factory.ResetDatabaseAsync(db => SeedScenarioAsync(db, user1Credit: 0m, user2Credit: 2m));
        var adminClient = _factory.CreatePowerUserClient(1);
        var userClient = _factory.CreateUserClient(2);

        var topUpResponse = await adminClient.PostAsJsonAsync("/admin/credito/ricariche", new CreditoTopUpRequestDTO
        {
            UserId = 2,
            Importo = 15m,
            Note = "Ricarica test"
        });

        topUpResponse.EnsureSuccessStatusCode();
        var topUpPayload = await topUpResponse.Content.ReadFromJsonAsync<CreditoTopUpResultDTO>();
        Assert.NotNull(topUpPayload);
        Assert.Equal(17m, topUpPayload.Utente.CreditoResiduo);
        Assert.Equal("TopUp", topUpPayload.Movimento.Tipo);
        Assert.Equal("user1@checkout.com", topUpPayload.Movimento.OperatoreEmail);

        var searchResponse = await adminClient.GetAsync("/admin/credito/users?email=user2@checkout.com");
        searchResponse.EnsureSuccessStatusCode();
        var searchPayload = await searchResponse.Content.ReadFromJsonAsync<List<CreditoUserLookupDTO>>();
        Assert.NotNull(searchPayload);
        Assert.Single(searchPayload);
        Assert.Equal(17m, searchPayload[0].CreditoResiduo);

        var historyResponse = await userClient.GetAsync("/credito/me");
        historyResponse.EnsureSuccessStatusCode();
        var historyPayload = await historyResponse.Content.ReadFromJsonAsync<CreditoMeDTO>();
        Assert.NotNull(historyPayload);
        Assert.Equal(17m, historyPayload.SaldoAttuale);
        Assert.Single(historyPayload.Movimenti);
        Assert.Equal("TopUp", historyPayload.Movimenti[0].Tipo);
    }

    /// <summary>Verifica lo scenario di PG7_CancelPendingOrder_ReleasesHeldSeatsAndCancelsOrder: predispone i dati e le condizioni previste dal caso di test e controlla che l'esito atteso venga restituito.</summary>
    [Fact]
    public async Task PG7_CancelPendingOrder_ReleasesHeldSeatsAndCancelsOrder()
    {
        await _factory.ResetDatabaseAsync(db => SeedScenarioAsync(db, user1Credit: 0m));
        var client = _factory.CreateUserClient(1);

        var order = await CreatePendingOrderAsync(client, new[] { 1, 2 });

        var cancelResponse = await client.PostAsync($"/checkout/orders/{order.Id}/cancel", null);
        cancelResponse.EnsureSuccessStatusCode();

        var cancelledOrder = await cancelResponse.Content.ReadFromJsonAsync<OrdineSummaryDTO>();
        Assert.NotNull(cancelledOrder);
        Assert.Equal("Cancelled", cancelledOrder.Stato);

        var seatMapResponse = await client.GetAsync("/checkout/shows/1/seat-map");
        seatMapResponse.EnsureSuccessStatusCode();
        var seatMap = await seatMapResponse.Content.ReadFromJsonAsync<SeatMapDTO>();
        Assert.NotNull(seatMap);
        Assert.All(seatMap.Posti.Where(p => p.SalaPostoId == 1 || p.SalaPostoId == 2), p => Assert.Equal(SeatStatus.Available, p.Stato));
    }

    private static async Task<OrdineSummaryDTO> CreatePendingOrderAsync(HttpClient client, IEnumerable<int> seatIds)
    {
        var holdResponse = await client.PostAsJsonAsync("/checkout/holds", new SeatHoldRequestDTO
        {
            ShowId = 1,
            SalaPostoIds = seatIds.ToList()
        });
        holdResponse.EnsureSuccessStatusCode();

        var holdPayload = await holdResponse.Content.ReadFromJsonAsync<SeatHoldResponseDTO>();
        Assert.NotNull(holdPayload);

        var orderResponse = await client.PostAsJsonAsync("/checkout/orders", new CreateOrdineRequestDTO
        {
            HoldToken = holdPayload.HoldToken,
            IdempotencyKey = $"order-{Guid.NewGuid():N}"
        });
        orderResponse.EnsureSuccessStatusCode();

        var orderPayload = await orderResponse.Content.ReadFromJsonAsync<OrdineSummaryDTO>();
        Assert.NotNull(orderPayload);
        return orderPayload;
    }

    private static async Task SeedScenarioAsync(FilmDbContext db, decimal user1Credit, decimal user2Credit = 0m, int seatCount = 5)
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

        db.Users.Add(new User
        {
            Email = "user1@checkout.com",
            PasswordHash = "hash",
            Nome = "User",
            Cognome = "One",
            Ruolo = UserRole.User,
            DataRegistrazione = DateTime.UtcNow,
            CreditoResiduo = user1Credit
        });

        db.Users.Add(new User
        {
            Email = "user2@checkout.com",
            PasswordHash = "hash",
            Nome = "User",
            Cognome = "Two",
            Ruolo = UserRole.User,
            DataRegistrazione = DateTime.UtcNow,
            CreditoResiduo = user2Credit
        });
        await db.SaveChangesAsync();

        db.Shows.Add(new Show
        {
            CinemaId = cinema.Id,
            SalaId = sala.Id,
            FilmId = film.Id,
            StartAtUtc = DateTime.UtcNow.AddHours(1),
            DurataMinutiSnapshot = 120,
            PrezzoBase = 10m,
            SupplementoSala = 0m
        });
        await db.SaveChangesAsync();
    }
}
