using System.Net;
using System.Net.Http.Json;
using System.Text;
using FilmAPI.Data;
using FilmAPI.DTO;
using FilmAPI.Model;
using FilmAPI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FilmAPI.Tests.Integration;

public class CheckoutHostedIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public CheckoutHostedIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private static async Task SeedScenarioAsync(FilmDbContext db, decimal user1Credit = 50m)
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

        for (int i = 1; i <= 10; i++)
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

        var show = new Show
        {
            CinemaId = cinema.Id,
            SalaId = sala.Id,
            FilmId = film.Id,
            StartAtUtc = DateTime.UtcNow.AddDays(1),
            DurataMinutiSnapshot = 120,
            PrezzoBase = 10m,
            SupplementoSala = 0m
        };
        db.Shows.Add(show);
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
            CreditoResiduo = user1Credit
        });
        await db.SaveChangesAsync();
    }

    private async Task<OrdineSummaryDTO> CreatePendingOrderAsync(HttpClient client, List<int> postoIds)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FilmDbContext>();
        var show = await db.Shows.FirstAsync();

        var holdResponse = await client.PostAsJsonAsync("/checkout/holds", new SeatHoldRequestDTO
        {
            ShowId = show.Id,
            SalaPostoIds = postoIds
        });
        holdResponse.EnsureSuccessStatusCode();
        var hold = await holdResponse.Content.ReadFromJsonAsync<SeatHoldResponseDTO>();

        var orderResponse = await client.PostAsJsonAsync("/checkout/orders", new CreateOrdineRequestDTO
        {
            HoldToken = hold!.HoldToken
        });
        orderResponse.EnsureSuccessStatusCode();
        return (await orderResponse.Content.ReadFromJsonAsync<OrdineSummaryDTO>())!;
    }

    [Fact]
    public async Task CH8A_CreateCheckoutSession_Carta_RedirectsToStripe()
    {
        await _factory.ResetDatabaseAsync(db => SeedScenarioAsync(db, user1Credit: 0m));
        var client = _factory.CreateUserClient(1);

        var order = await CreatePendingOrderAsync(client, new List<int> { 1, 2 });

        var response = await client.PostAsJsonAsync($"/checkout/orders/{order.Id}/stripe-checkout-session", new CreateCheckoutSessionRequestDTO
        {
            MetodoPagamento = "Carta"
        });
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<CreateCheckoutSessionResponseDTO>();
        Assert.NotNull(result);
        Assert.NotEmpty(result.StripeCheckoutSessionId);
        Assert.NotEmpty(result.StripeCheckoutUrl);
        Assert.Equal(20m, result.ImportoCarta);
        Assert.Equal(0m, result.ImportoCredito);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FilmDbContext>();
        var ordine = await db.Ordini.FirstAsync(o => o.Id == order.Id);
        Assert.Equal(OrdineState.CheckoutInProgress, ordine.Stato);
        Assert.NotNull(ordine.StripeCheckoutSessionId);
        Assert.NotNull(ordine.CheckoutExpiresAtUtc);
    }

    [Fact]
    public async Task CH8B_GetCheckoutStatus_ReturnsCheckoutInProgress()
    {
        await _factory.ResetDatabaseAsync(db => SeedScenarioAsync(db, user1Credit: 0m));
        var client = _factory.CreateUserClient(1);

        var order = await CreatePendingOrderAsync(client, new List<int> { 3, 4 });

        var sessionResponse = await client.PostAsJsonAsync($"/checkout/orders/{order.Id}/stripe-checkout-session", new CreateCheckoutSessionRequestDTO
        {
            MetodoPagamento = "Carta"
        });
        sessionResponse.EnsureSuccessStatusCode();

        var statusResponse = await client.GetAsync($"/checkout/orders/{order.Id}/checkout-status");
        statusResponse.EnsureSuccessStatusCode();

        var status = await statusResponse.Content.ReadFromJsonAsync<CheckoutStatusDTO>();
        Assert.NotNull(status);
        Assert.Equal("CheckoutInProgress", status.Stato);
        Assert.Equal(order.Id, status.OrdineId);
        Assert.NotNull(status.StripeCheckoutSessionId);
    }

    [Fact]
    public async Task CH8C_CheckoutSessionCompleted_WebhookFinalizesOrder()
    {
        await _factory.ResetDatabaseAsync(db => SeedScenarioAsync(db, user1Credit: 0m));
        var client = _factory.CreateUserClient(1);

        var order = await CreatePendingOrderAsync(client, new List<int> { 5, 6 });

        var sessionResponse = await client.PostAsJsonAsync($"/checkout/orders/{order.Id}/stripe-checkout-session", new CreateCheckoutSessionRequestDTO
        {
            MetodoPagamento = "Carta"
        });
        sessionResponse.EnsureSuccessStatusCode();
        var session = await sessionResponse.Content.ReadFromJsonAsync<CreateCheckoutSessionResponseDTO>();

        _factory.StripeGateway.SetCheckoutSessionStatus(session!.StripeCheckoutSessionId, "complete", "pi_test_completed");

        var (webhookPayload, webhookSignature) = _factory.StripeGateway.CreateCheckoutWebhook("evt_test_completed", "checkout.session.completed", session.StripeCheckoutSessionId);

        var webhookClient = _factory.CreateClient();
        webhookClient.DefaultRequestHeaders.Add("Stripe-Signature", webhookSignature);
        var webhookResponse = await webhookClient.PostAsync("/payments/stripe/webhook",
            new StringContent(webhookPayload, Encoding.UTF8, "application/json"));

        webhookResponse.EnsureSuccessStatusCode();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FilmDbContext>();
        var ordine = await db.Ordini.Include(o => o.Biglietti).FirstAsync(o => o.Id == order.Id);

        Assert.Equal(OrdineState.Paid, ordine.Stato);
        Assert.NotNull(ordine.PaidAtUtc);
        Assert.Equal(2, ordine.Biglietti.Count);

        var postoStati = await db.ShowPostiStato.Where(sps => sps.OrdineId == order.Id).ToListAsync();
        Assert.All(postoStati, ps => Assert.Equal(ShowPostoState.Sold, ps.Stato));
    }

    [Fact]
    public async Task CH8D_CheckoutSessionExpired_WebhookReleasesHolds()
    {
        await _factory.ResetDatabaseAsync(db => SeedScenarioAsync(db, user1Credit: 0m));
        var client = _factory.CreateUserClient(1);

        var order = await CreatePendingOrderAsync(client, new List<int> { 7, 8 });

        var sessionResponse = await client.PostAsJsonAsync($"/checkout/orders/{order.Id}/stripe-checkout-session", new CreateCheckoutSessionRequestDTO
        {
            MetodoPagamento = "Carta"
        });
        sessionResponse.EnsureSuccessStatusCode();
        var session = await sessionResponse.Content.ReadFromJsonAsync<CreateCheckoutSessionResponseDTO>();

        _factory.StripeGateway.SetCheckoutSessionStatus(session!.StripeCheckoutSessionId, "expired");

        var (webhookPayload, webhookSignature) = _factory.StripeGateway.CreateCheckoutWebhook("evt_test_expired", "checkout.session.expired", session.StripeCheckoutSessionId);

        var webhookClient = _factory.CreateClient();
        webhookClient.DefaultRequestHeaders.Add("Stripe-Signature", webhookSignature);
        var webhookResponse = await webhookClient.PostAsync("/payments/stripe/webhook",
            new StringContent(webhookPayload, Encoding.UTF8, "application/json"));

        webhookResponse.EnsureSuccessStatusCode();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FilmDbContext>();
        var ordine = await db.Ordini.FirstAsync(o => o.Id == order.Id);
        Assert.Equal(OrdineState.Expired, ordine.Stato);

        var postoStati = await db.ShowPostiStato.Where(sps => sps.OrdineId == order.Id).ToListAsync();
        Assert.Empty(postoStati);
    }

    [Fact]
    public async Task CH8E_ReconcileCheckoutSession_UpdatesStateFromStripe()
    {
        await _factory.ResetDatabaseAsync(db => SeedScenarioAsync(db, user1Credit: 0m));
        var client = _factory.CreateUserClient(1);

        var order = await CreatePendingOrderAsync(client, new List<int> { 9, 10 });

        var sessionResponse = await client.PostAsJsonAsync($"/checkout/orders/{order.Id}/stripe-checkout-session", new CreateCheckoutSessionRequestDTO
        {
            MetodoPagamento = "Carta"
        });
        sessionResponse.EnsureSuccessStatusCode();
        var session = await sessionResponse.Content.ReadFromJsonAsync<CreateCheckoutSessionResponseDTO>();

        _factory.StripeGateway.SetCheckoutSessionStatus(session!.StripeCheckoutSessionId, "complete", "pi_test_reconciled");

        var reconcileResponse = await client.PostAsync($"/checkout/orders/{order.Id}/reconcile-checkout-session", null);
        reconcileResponse.EnsureSuccessStatusCode();

        var status = await reconcileResponse.Content.ReadFromJsonAsync<CheckoutStatusDTO>();
        Assert.NotNull(status);
        Assert.Equal("Paid", status.Stato);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FilmDbContext>();
        var ordine = await db.Ordini.FirstAsync(o => o.Id == order.Id);
        Assert.Equal(OrdineState.Paid, ordine.Stato);
    }

    [Fact]
    public async Task CH8F_DuplicateWebhook_DoesNotDuplicateTickets()
    {
        await _factory.ResetDatabaseAsync(db => SeedScenarioAsync(db, user1Credit: 0m));
        var client = _factory.CreateUserClient(1);

        var order = await CreatePendingOrderAsync(client, new List<int> { 1 });

        var sessionResponse = await client.PostAsJsonAsync($"/checkout/orders/{order.Id}/stripe-checkout-session", new CreateCheckoutSessionRequestDTO
        {
            MetodoPagamento = "Carta"
        });
        sessionResponse.EnsureSuccessStatusCode();
        var session = await sessionResponse.Content.ReadFromJsonAsync<CreateCheckoutSessionResponseDTO>();

        _factory.StripeGateway.SetCheckoutSessionStatus(session!.StripeCheckoutSessionId, "complete", "pi_test_duplicate");

        var (webhookPayload, webhookSignature) = _factory.StripeGateway.CreateCheckoutWebhook("evt_test_dup", "checkout.session.completed", session.StripeCheckoutSessionId);

        var webhookClient = _factory.CreateClient();
        webhookClient.DefaultRequestHeaders.Add("Stripe-Signature", webhookSignature);

        await webhookClient.PostAsync("/payments/stripe/webhook",
            new StringContent(webhookPayload, Encoding.UTF8, "application/json"));

        await webhookClient.PostAsync("/payments/stripe/webhook",
            new StringContent(webhookPayload, Encoding.UTF8, "application/json"));

        await webhookClient.PostAsync("/payments/stripe/webhook",
            new StringContent(webhookPayload, Encoding.UTF8, "application/json"));

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FilmDbContext>();
        var ordine = await db.Ordini.Include(o => o.Biglietti).FirstAsync(o => o.Id == order.Id);

        Assert.Equal(OrdineState.Paid, ordine.Stato);
        Assert.Single(ordine.Biglietti);
    }

    [Fact]
    public async Task CH8G_CancelPendingOrdine_WhileCheckoutInProgress()
    {
        await _factory.ResetDatabaseAsync(db => SeedScenarioAsync(db, user1Credit: 0m));
        var client = _factory.CreateUserClient(1);

        var order = await CreatePendingOrderAsync(client, new List<int> { 2 });

        var sessionResponse = await client.PostAsJsonAsync($"/checkout/orders/{order.Id}/stripe-checkout-session", new CreateCheckoutSessionRequestDTO
        {
            MetodoPagamento = "Carta"
        });
        sessionResponse.EnsureSuccessStatusCode();

        var cancelResponse = await client.PostAsync($"/checkout/orders/{order.Id}/cancel", null);
        cancelResponse.EnsureSuccessStatusCode();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FilmDbContext>();
        var ordine = await db.Ordini.FirstAsync(o => o.Id == order.Id);
        Assert.Equal(OrdineState.Cancelled, ordine.Stato);

        var postoStati = await db.ShowPostiStato.Where(sps => sps.OrdineId == order.Id).ToListAsync();
        Assert.Empty(postoStati);
    }

    [Fact]
    public async Task CH8H_CreateCheckoutSession_Mixed_ReservesCreditAndChargesOnlyResidualCard()
    {
        await _factory.ResetDatabaseAsync(db => SeedScenarioAsync(db, user1Credit: 7m));
        var client = _factory.CreateUserClient(1);

        var order = await CreatePendingOrderAsync(client, new List<int> { 3, 4 });

        var response = await client.PostAsJsonAsync($"/checkout/orders/{order.Id}/stripe-checkout-session", new CreateCheckoutSessionRequestDTO
        {
            MetodoPagamento = "Misto",
            ImportoCreditoRichiesto = 7m
        });
        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());

        var result = await response.Content.ReadFromJsonAsync<CreateCheckoutSessionResponseDTO>();
        Assert.NotNull(result);
        Assert.Equal(13m, result.ImportoCarta);
        Assert.Equal(7m, result.ImportoCredito);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FilmDbContext>();
        var ordine = await db.Ordini.FirstAsync(o => o.Id == order.Id);
        var user = await db.Users.FirstAsync(u => u.Id == 1);
        var reserveMovement = await db.MovimentiCredito.SingleAsync(m => m.OrdineId == order.Id && m.Tipo == MovimentoCreditoTipo.Adjustment);

        Assert.Equal(OrdineState.CheckoutInProgress, ordine.Stato);
        Assert.Equal(7m, ordine.CreditoRiservato);
        Assert.Equal(7m, ordine.ImportoCredito);
        Assert.Equal(13m, ordine.ImportoCarta);
        Assert.Equal(0m, user.CreditoResiduo);
        Assert.Equal(-7m, reserveMovement.Importo);
    }

    [Fact]
    public async Task CH8I_ExpiredMixedCheckout_ReleasesReservedCredit()
    {
        await _factory.ResetDatabaseAsync(db => SeedScenarioAsync(db, user1Credit: 7m));
        var client = _factory.CreateUserClient(1);

        var order = await CreatePendingOrderAsync(client, new List<int> { 5, 6 });

        var sessionResponse = await client.PostAsJsonAsync($"/checkout/orders/{order.Id}/stripe-checkout-session", new CreateCheckoutSessionRequestDTO
        {
            MetodoPagamento = "Misto",
            ImportoCreditoRichiesto = 7m
        });
        Assert.True(sessionResponse.IsSuccessStatusCode, await sessionResponse.Content.ReadAsStringAsync());
        var session = await sessionResponse.Content.ReadFromJsonAsync<CreateCheckoutSessionResponseDTO>();

        _factory.StripeGateway.SetCheckoutSessionStatus(session!.StripeCheckoutSessionId, "expired");
        var (webhookPayload, webhookSignature) = _factory.StripeGateway.CreateCheckoutWebhook("evt_test_mixed_expired", "checkout.session.expired", session.StripeCheckoutSessionId);

        var webhookClient = _factory.CreateClient();
        webhookClient.DefaultRequestHeaders.Add("Stripe-Signature", webhookSignature);
        var webhookResponse = await webhookClient.PostAsync("/payments/stripe/webhook",
            new StringContent(webhookPayload, Encoding.UTF8, "application/json"));

        webhookResponse.EnsureSuccessStatusCode();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FilmDbContext>();
        var ordine = await db.Ordini.FirstAsync(o => o.Id == order.Id);
        var user = await db.Users.FirstAsync(u => u.Id == 1);
        var releaseMovement = await db.MovimentiCredito.SingleAsync(m => m.OrdineId == order.Id && m.Tipo == MovimentoCreditoTipo.Refund);

        Assert.Equal(OrdineState.Expired, ordine.Stato);
        Assert.Equal(0m, ordine.CreditoRiservato);
        Assert.Equal(7m, user.CreditoResiduo);
        Assert.Equal(7m, releaseMovement.Importo);
    }

    [Fact]
    public async Task CH8J_CompletedMixedCheckout_FinalizesOrderWithoutDoubleDebitingCredit()
    {
        await _factory.ResetDatabaseAsync(db => SeedScenarioAsync(db, user1Credit: 7m));
        var client = _factory.CreateUserClient(1);

        var order = await CreatePendingOrderAsync(client, new List<int> { 7, 8 });

        var sessionResponse = await client.PostAsJsonAsync($"/checkout/orders/{order.Id}/stripe-checkout-session", new CreateCheckoutSessionRequestDTO
        {
            MetodoPagamento = "Misto",
            ImportoCreditoRichiesto = 7m
        });
        Assert.True(sessionResponse.IsSuccessStatusCode, await sessionResponse.Content.ReadAsStringAsync());
        var session = await sessionResponse.Content.ReadFromJsonAsync<CreateCheckoutSessionResponseDTO>();

        _factory.StripeGateway.SetCheckoutSessionStatus(session!.StripeCheckoutSessionId, "complete", "pi_test_mixed_complete");
        var (webhookPayload, webhookSignature) = _factory.StripeGateway.CreateCheckoutWebhook("evt_test_mixed_complete", "checkout.session.completed", session.StripeCheckoutSessionId);

        var webhookClient = _factory.CreateClient();
        webhookClient.DefaultRequestHeaders.Add("Stripe-Signature", webhookSignature);
        var webhookResponse = await webhookClient.PostAsync("/payments/stripe/webhook",
            new StringContent(webhookPayload, Encoding.UTF8, "application/json"));

        webhookResponse.EnsureSuccessStatusCode();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FilmDbContext>();
        var ordine = await db.Ordini.Include(o => o.Biglietti).FirstAsync(o => o.Id == order.Id);
        var user = await db.Users.FirstAsync(u => u.Id == 1);
        var debitMovements = await db.MovimentiCredito.Where(m => m.OrdineId == order.Id && m.Tipo == MovimentoCreditoTipo.DebitOrder).ToListAsync();
        var reserveMovements = await db.MovimentiCredito.Where(m => m.OrdineId == order.Id && m.Tipo == MovimentoCreditoTipo.Adjustment).ToListAsync();

        Assert.Equal(OrdineState.Paid, ordine.Stato);
        Assert.Equal(7m, ordine.ImportoCredito);
        Assert.Equal(13m, ordine.ImportoCarta);
        Assert.Equal(0m, ordine.CreditoRiservato);
        Assert.NotNull(ordine.CheckoutCompletedAtUtc);
        Assert.Equal(0m, user.CreditoResiduo);
        Assert.Empty(debitMovements);
        Assert.Single(reserveMovements);
        Assert.Equal(2, ordine.Biglietti.Count);
    }

    [Fact]
    public async Task CH8K_PaymentIntentFailed_DuringHostedCheckout_DoesNotReleaseSeatsOrExpireOrder()
    {
        await _factory.ResetDatabaseAsync(db => SeedScenarioAsync(db, user1Credit: 0m));
        var client = _factory.CreateUserClient(1);

        var order = await CreatePendingOrderAsync(client, new List<int> { 1, 2 });

        var sessionResponse = await client.PostAsJsonAsync($"/checkout/orders/{order.Id}/stripe-checkout-session", new CreateCheckoutSessionRequestDTO
        {
            MetodoPagamento = "Carta"
        });
        sessionResponse.EnsureSuccessStatusCode();
        var session = await sessionResponse.Content.ReadFromJsonAsync<CreateCheckoutSessionResponseDTO>();

        _factory.StripeGateway.SetCheckoutSessionStatus(session!.StripeCheckoutSessionId, "open", "pi_test_failed_open");
        var webhook = new StripeWebhookEvent
        {
            EventId = "evt_test_pi_failed_hosted",
            EventType = "payment_intent.payment_failed",
            PaymentIntent = new StripePaymentIntentSnapshot
            {
                Id = "pi_test_failed_open",
                Status = "requires_payment_method",
                Metadata = new Dictionary<string, string> { ["orderId"] = order.Id.ToString() }
            }
        };

        var webhookClient = _factory.CreateClient();
        webhookClient.DefaultRequestHeaders.Add("Stripe-Signature", "test-stripe-signature");
        var webhookContent = new StringContent(System.Text.Json.JsonSerializer.Serialize(webhook), Encoding.UTF8);
        webhookContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
        var webhookResponse = await webhookClient.PostAsync("/payments/stripe/webhook", webhookContent);

        webhookResponse.EnsureSuccessStatusCode();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FilmDbContext>();
        var ordine = await db.Ordini.FirstAsync(o => o.Id == order.Id);
        var postoStati = await db.ShowPostiStato.Where(sps => sps.OrdineId == order.Id).ToListAsync();

        Assert.Equal(OrdineState.CheckoutInProgress, ordine.Stato);
        Assert.Equal(2, postoStati.Count);
        Assert.All(postoStati, ps => Assert.Equal(ShowPostoState.Hold, ps.Stato));
    }

    [Fact]
    public async Task CH8L_PaymentIntentCanceled_DuringHostedCheckout_DoesNotReleaseSeatsOrExpireOrder()
    {
        await _factory.ResetDatabaseAsync(db => SeedScenarioAsync(db, user1Credit: 0m));
        var client = _factory.CreateUserClient(1);

        var order = await CreatePendingOrderAsync(client, new List<int> { 3, 4 });

        var sessionResponse = await client.PostAsJsonAsync($"/checkout/orders/{order.Id}/stripe-checkout-session", new CreateCheckoutSessionRequestDTO
        {
            MetodoPagamento = "Carta"
        });
        sessionResponse.EnsureSuccessStatusCode();
        var session = await sessionResponse.Content.ReadFromJsonAsync<CreateCheckoutSessionResponseDTO>();

        _factory.StripeGateway.SetCheckoutSessionStatus(session!.StripeCheckoutSessionId, "open", "pi_test_canceled_open");
        var webhook = new StripeWebhookEvent
        {
            EventId = "evt_test_pi_canceled_hosted",
            EventType = "payment_intent.canceled",
            PaymentIntent = new StripePaymentIntentSnapshot
            {
                Id = "pi_test_canceled_open",
                Status = "canceled",
                Metadata = new Dictionary<string, string> { ["orderId"] = order.Id.ToString() }
            }
        };

        var webhookClient = _factory.CreateClient();
        webhookClient.DefaultRequestHeaders.Add("Stripe-Signature", "test-stripe-signature");
        var webhookContent = new StringContent(System.Text.Json.JsonSerializer.Serialize(webhook), Encoding.UTF8);
        webhookContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
        var webhookResponse = await webhookClient.PostAsync("/payments/stripe/webhook", webhookContent);

        webhookResponse.EnsureSuccessStatusCode();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FilmDbContext>();
        var ordine = await db.Ordini.FirstAsync(o => o.Id == order.Id);
        var postoStati = await db.ShowPostiStato.Where(sps => sps.OrdineId == order.Id).ToListAsync();

        Assert.Equal(OrdineState.CheckoutInProgress, ordine.Stato);
        Assert.Equal(2, postoStati.Count);
        Assert.All(postoStati, ps => Assert.Equal(ShowPostoState.Hold, ps.Stato));
    }

    [Fact]
    public async Task CH8M_CreateCheckoutSession_WithInvalidPayload_ReturnsBadRequest()
    {
        await _factory.ResetDatabaseAsync(db => SeedScenarioAsync(db, user1Credit: 10m));
        var client = _factory.CreateUserClient(1);

        var order = await CreatePendingOrderAsync(client, new List<int> { 5, 6 });

        var response = await client.PostAsJsonAsync($"/checkout/orders/{order.Id}/stripe-checkout-session", new CreateCheckoutSessionRequestDTO
        {
            MetodoPagamento = "Misto",
            ImportoCreditoRichiesto = 20m
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
