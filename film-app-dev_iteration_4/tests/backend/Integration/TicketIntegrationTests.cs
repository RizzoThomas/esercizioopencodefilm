using System.Net;
using System.Net.Http.Json;
using FilmAPI.Data;
using FilmAPI.DTO;
using FilmAPI.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace FilmAPI.Tests.Integration;

public class TicketIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public TicketIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TK1_PayOrder_WithCredit_EmitsTicketsAndSendsEmailWithPdfAttachment()
    {
        await _factory.ResetDatabaseAsync(db => SeedScenarioAsync(db, user1Credit: 25m));
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
        Assert.Equal(2, payload.Ordine.Biglietti.Count);
        Assert.All(payload.Ordine.Biglietti, ticket => Assert.StartsWith("CB-", ticket.CodiceBiglietto));
        Assert.NotNull(payload.Ordine.TicketEmailSentAtUtc);
        Assert.Null(payload.Ordine.TicketEmailLastError);

        var sentEmails = _factory.EmailService.SentEmails;
        Assert.Single(sentEmails);
        Assert.Equal(order.Id, sentEmails[0].OrderId);
        Assert.Equal("user1@checkout.com", sentEmails[0].RecipientEmail);
        Assert.Equal(2, sentEmails[0].TicketCodes.Count);
        Assert.EndsWith($"{payload.Ordine.CodiceOrdine}.pdf", sentEmails[0].FileName);
        Assert.StartsWith("%PDF", System.Text.Encoding.ASCII.GetString(sentEmails[0].PdfBytes, 0, 4));

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FilmDbContext>();
        var dbOrder = await db.Ordini.Include(o => o.Biglietti).SingleAsync(o => o.Id == order.Id);
        Assert.Equal(OrdineState.Paid, dbOrder.Stato);
        Assert.Equal(2, dbOrder.Biglietti.Count);
        Assert.NotNull(dbOrder.TicketEmailSentAtUtc);
        Assert.Null(dbOrder.TicketEmailLastError);
    }

    [Fact]
    public async Task TK2_DownloadOrderPdf_ReturnsMultipagePdfContainingRequiredData()
    {
        await _factory.ResetDatabaseAsync(db => SeedScenarioAsync(db, user1Credit: 25m));
        var client = _factory.CreateUserClient(1);

        var order = await CreatePendingOrderAsync(client, new[] { 1, 2 });
        var payResponse = await client.PostAsJsonAsync($"/checkout/orders/{order.Id}/pay", new PayOrdineRequestDTO
        {
            MetodoPagamento = "Credito"
        });
        payResponse.EnsureSuccessStatusCode();

        var pdfResponse = await client.GetAsync($"/checkout/orders/{order.Id}/pdf");

        pdfResponse.EnsureSuccessStatusCode();
        Assert.Equal("application/pdf", pdfResponse.Content.Headers.ContentType?.MediaType);
        var pdfBytes = await pdfResponse.Content.ReadAsByteArrayAsync();
        Assert.StartsWith("%PDF", System.Text.Encoding.ASCII.GetString(pdfBytes, 0, 4));

        using var document = PdfDocument.Open(pdfBytes);
        Assert.Equal(2, document.NumberOfPages);

        var fullText = string.Join("\n", document.GetPages().Select(page => ContentOrderTextExtractor.GetText(page)));
        Assert.Contains("CineBase - Biglietto digitale", fullText);
        Assert.Contains("Film Test", fullText);
        Assert.Contains("Cinema Test", fullText);
        Assert.Contains("Via Test 1", fullText);
        Assert.Contains("ROMA-CENTRO", fullText);
        Assert.Contains("Sala 1", fullText);
        Assert.Contains("Prezzo base: 10,00 EUR", fullText);
        Assert.Contains("Supplemento: 1,50 EUR", fullText);
        Assert.Contains("Totale: 11,50 EUR", fullText);
        Assert.Contains("URL validazione:", fullText);
    }

    [Fact]
    public async Task TK3_DownloadOrderPdf_OtherUser_ReturnsNotFound()
    {
        await _factory.ResetDatabaseAsync(db => SeedScenarioAsync(db, user1Credit: 25m));
        var ownerClient = _factory.CreateUserClient(1);
        var otherClient = _factory.CreateUserClient(2);

        var order = await CreatePendingOrderAsync(ownerClient, new[] { 1 });
        var payResponse = await ownerClient.PostAsJsonAsync($"/checkout/orders/{order.Id}/pay", new PayOrdineRequestDTO
        {
            MetodoPagamento = "Credito"
        });
        payResponse.EnsureSuccessStatusCode();

        var pdfResponse = await otherClient.GetAsync($"/checkout/orders/{order.Id}/pdf");

        Assert.Equal(HttpStatusCode.NotFound, pdfResponse.StatusCode);
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
            IdempotencyKey = $"ticket-order-{Guid.NewGuid():N}"
        });
        orderResponse.EnsureSuccessStatusCode();

        var orderPayload = await orderResponse.Content.ReadFromJsonAsync<OrdineSummaryDTO>();
        Assert.NotNull(orderPayload);
        return orderPayload;
    }

    private static async Task SeedScenarioAsync(FilmDbContext db, decimal user1Credit, int seatCount = 5)
    {
        var cinema = new Cinema
        {
            Nome = "Cinema Test",
            Citta = "Roma",
            Indirizzo = "Via Test 1",
            CodiceLocale = "ROMA-CENTRO"
        };
        db.Cinemas.Add(cinema);

        var otherCinema = new Cinema
        {
            Nome = "Cinema Altro",
            Citta = "Milano",
            Indirizzo = "Via Altra 99",
            CodiceLocale = "MIL-NORD"
        };
        db.Cinemas.Add(otherCinema);
        await db.SaveChangesAsync();

        var sala = new Sala
        {
            CinemaId = cinema.Id,
            NumeroProgressivo = 1,
            TipoSala = TipoSala.DueD,
            Nome = "Sala 1",
            Supplemento = 1.5m,
            IsAttiva = true
        };
        db.Sale.Add(sala);
        await db.SaveChangesAsync();

        for (var i = 1; i <= seatCount; i++)
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

        db.Users.Add(new User
        {
            Id = 1,
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
            Id = 2,
            Email = "user2@checkout.com",
            PasswordHash = "hash",
            Nome = "User",
            Cognome = "Two",
            Ruolo = UserRole.User,
            DataRegistrazione = DateTime.UtcNow,
            CreditoResiduo = 0m
        });
        await db.SaveChangesAsync();

        db.Shows.Add(new Show
        {
            Id = 1,
            CinemaId = cinema.Id,
            SalaId = sala.Id,
            FilmId = film.Id,
            StartAtUtc = new DateTime(2026, 4, 18, 18, 30, 0, DateTimeKind.Utc),
            DurataMinutiSnapshot = 120,
            PrezzoBase = 10m,
            SupplementoSala = 1.5m
        });
        await db.SaveChangesAsync();
    }
}
