using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using FilmAPI.Data;
using FilmAPI.DTO;
using FilmAPI.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FilmAPI.Tests.Integration;

public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly FakeStripePaymentGateway _stripeGateway = new();
    private readonly FakeEmailService _emailService = new();

    public CustomWebApplicationFactory()
    {
        Environment.SetEnvironmentVariable("DB_USE_AUTODETECT", "false");
        Environment.SetEnvironmentVariable("DB_SERVER_VERSION", "10.11.0-mariadb");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseWebRoot("wwwroot");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IDbContextOptionsConfiguration<FilmDbContext>>();
            services.RemoveAll<DbContextOptions<FilmDbContext>>();
            services.RemoveAll<FilmDbContext>();
            services.RemoveAll<IStripePaymentGateway>();
            services.RemoveAll<IEmailService>();

            services.AddDbContext<FilmDbContext>(options =>
            options.UseSqlite(_connection));
            services.AddSingleton<IStripePaymentGateway>(_stripeGateway);
            services.AddSingleton<IEmailService>(_emailService);

            services.RemoveAll<IConfigureOptions<AuthenticationOptions>>();

            services.AddAuthentication("Test")
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", options => { });

            using var scope = services.BuildServiceProvider().CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<FilmDbContext>();
            db.Database.EnsureCreated();
        });
    }

    public async Task InitializeAsync()
    {
        await _connection.OpenAsync();
    }

    public new async Task DisposeAsync()
    {
        await _connection.DisposeAsync();
    }

    public async Task ResetDatabaseAsync(Func<FilmDbContext, Task>? seed = null)
    {
        _stripeGateway.Reset();
        _emailService.Reset();

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FilmDbContext>();
        await db.Database.EnsureDeletedAsync();
        await db.Database.EnsureCreatedAsync();

        if (seed is not null)
        {
            await seed(db);
            await db.SaveChangesAsync();
        }
    }

    public HttpClient CreateAdminClient(int userId = 1)
    {
        var client = CreateDefaultClient();
        client.DefaultRequestHeaders.Add("X-Test-Role", "Admin");
        client.DefaultRequestHeaders.Add("X-Test-UserId", userId.ToString());
        return client;
    }

    public HttpClient CreatePowerUserClient(int userId = 1)
    {
        var client = CreateDefaultClient();
        client.DefaultRequestHeaders.Add("X-Test-Role", "PowerUser");
        client.DefaultRequestHeaders.Add("X-Test-UserId", userId.ToString());
        return client;
    }

    public HttpClient CreateUserClient(int userId = 1)
    {
        var client = CreateDefaultClient();
        client.DefaultRequestHeaders.Add("X-Test-Role", "User");
        client.DefaultRequestHeaders.Add("X-Test-UserId", userId.ToString());
        return client;
    }

    public HttpClient CreateAnonymousClient()
    {
        return CreateDefaultClient();
    }

    public HttpClient CreateAuthenticatedClient(string role, int userId = 1, string email = "test@test.com", string nome = "Test")
    {
        var client = CreateDefaultClient();
        client.DefaultRequestHeaders.Add("X-Test-Role", role);
        client.DefaultRequestHeaders.Add("X-Test-UserId", userId.ToString());
        client.DefaultRequestHeaders.Add("X-Test-Email", email);
        client.DefaultRequestHeaders.Add("X-Test-Nome", nome);
        return client;
    }

    public FakeStripePaymentGateway StripeGateway => _stripeGateway;
    public FakeEmailService EmailService => _emailService;
}

public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.ContainsKey("X-Test-Role"))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var role = Request.Headers["X-Test-Role"].ToString();
        var userId = Request.Headers["X-Test-UserId"].FirstOrDefault() ?? "1";
        var email = Request.Headers["X-Test-Email"].FirstOrDefault() ?? "test@test.com";
        var nome = Request.Headers["X-Test-Nome"].FirstOrDefault() ?? "Test";

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Role, role),
            new Claim("sub", userId),
            new Claim("nome", nome)
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

public sealed class FakeStripePaymentGateway : IStripePaymentGateway
{
    private readonly object _lock = new();
    private readonly Dictionary<string, StripePaymentIntentSnapshot> _paymentIntents = new(StringComparer.Ordinal);
    private readonly Dictionary<string, StripeCheckoutSessionSnapshot> _checkoutSessions = new(StringComparer.Ordinal);
    private int _sequence;
    private const string ExpectedSignature = "test-stripe-signature";

    public Task<StripePaymentIntentSnapshot> CreatePaymentIntentAsync(StripeCreatePaymentIntentRequest request, string? idempotencyKey, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var id = $"pi_test_{++_sequence:D6}";
            var snapshot = new StripePaymentIntentSnapshot
            {
                Id = id,
                ClientSecret = $"{id}_secret_test",
                Status = "requires_confirmation",
                Amount = checked((long)decimal.Round(request.Amount * 100m, 0, MidpointRounding.AwayFromZero)),
                Currency = request.Currency,
                Metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["orderId"] = request.OrderId.ToString(),
                    ["orderCode"] = request.OrderCode,
                    ["userId"] = request.UserId.ToString(),
                    ["showId"] = request.ShowId.ToString()
                }
            };

            _paymentIntents[id] = Clone(snapshot);
            return Task.FromResult(Clone(snapshot));
        }
    }

    public Task<StripePaymentIntentSnapshot> GetPaymentIntentAsync(string paymentIntentId, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (!_paymentIntents.TryGetValue(paymentIntentId, out var snapshot))
                throw new InvalidOperationException("PaymentIntent non trovato nel fake gateway.");

            return Task.FromResult(Clone(snapshot));
        }
    }

    public Task<StripeCheckoutSessionSnapshot> CreateCheckoutSessionAsync(StripeCreateCheckoutSessionRequest request, string? idempotencyKey, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var id = $"cs_test_{++_sequence:D6}";
            var snapshot = new StripeCheckoutSessionSnapshot
            {
                Id = id,
                Url = $"https://checkout.stripe.com/test/{id}",
                Status = "open",
                AmountTotal = checked((long)decimal.Round(request.Amount * 100m, 0, MidpointRounding.AwayFromZero)),
                Currency = request.Currency,
                PaymentIntentId = null,
                ExpiresAt = DateTime.UtcNow.AddHours(1),
                Metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["orderId"] = request.OrderId.ToString(),
                    ["orderCode"] = request.OrderCode,
                    ["userId"] = request.UserId.ToString(),
                    ["showId"] = request.ShowId.ToString()
                }
            };

            _checkoutSessions[id] = CloneCheckout(snapshot);
            return Task.FromResult(CloneCheckout(snapshot));
        }
    }

    public Task<StripeCheckoutSessionSnapshot> GetCheckoutSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (!_checkoutSessions.TryGetValue(sessionId, out var snapshot))
                throw new InvalidOperationException("CheckoutSession non trovata nel fake gateway.");

            return Task.FromResult(CloneCheckout(snapshot));
        }
    }

    public StripeWebhookEvent ParseWebhookEvent(string payload, string? signatureHeader)
    {
        if (!string.Equals(signatureHeader, ExpectedSignature, StringComparison.Ordinal))
            throw new InvalidOperationException("Firma Stripe non valida.");

        var result = JsonSerializer.Deserialize<StripeWebhookEvent>(payload, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (result is null || string.IsNullOrWhiteSpace(result.EventType))
            throw new InvalidOperationException("Payload webhook non valido.");

        if (result.PaymentIntent is null && result.CheckoutSession is null)
            throw new InvalidOperationException("Payload webhook non valido.");

        return result;
    }

    public void SetPaymentIntentStatus(string paymentIntentId, string status)
    {
        lock (_lock)
        {
            if (!_paymentIntents.TryGetValue(paymentIntentId, out var snapshot))
                throw new InvalidOperationException("PaymentIntent non trovato nel fake gateway.");

            snapshot.Status = status;
        }
    }

    public void SetCheckoutSessionStatus(string sessionId, string status, string? paymentIntentId = null)
    {
        lock (_lock)
        {
            if (!_checkoutSessions.TryGetValue(sessionId, out var snapshot))
                throw new InvalidOperationException("CheckoutSession non trovata nel fake gateway.");

            snapshot.Status = status;
            if (paymentIntentId != null)
                snapshot.PaymentIntentId = paymentIntentId;
        }
    }

    public (string Payload, string Signature) CreateWebhook(string eventId, string eventType, string paymentIntentId)
    {
        lock (_lock)
        {
            if (!_paymentIntents.TryGetValue(paymentIntentId, out var snapshot))
                throw new InvalidOperationException("PaymentIntent non trovato nel fake gateway.");

            var webhook = new StripeWebhookEvent
            {
                EventId = eventId,
                EventType = eventType,
                PaymentIntent = Clone(snapshot)
            };

            var payload = JsonSerializer.Serialize(webhook);
            return (payload, ExpectedSignature);
        }
    }

    public (string Payload, string Signature) CreateCheckoutWebhook(string eventId, string eventType, string sessionId)
    {
        lock (_lock)
        {
            if (!_checkoutSessions.TryGetValue(sessionId, out var snapshot))
                throw new InvalidOperationException("CheckoutSession non trovata nel fake gateway.");

            var webhook = new StripeWebhookEvent
            {
                EventId = eventId,
                EventType = eventType,
                CheckoutSession = CloneCheckout(snapshot)
            };

            var payload = JsonSerializer.Serialize(webhook);
            return (payload, ExpectedSignature);
        }
    }

    public void Reset()
    {
        lock (_lock)
        {
            _paymentIntents.Clear();
            _checkoutSessions.Clear();
            _sequence = 0;
        }
    }

    private static StripePaymentIntentSnapshot Clone(StripePaymentIntentSnapshot snapshot)
    {
        return new StripePaymentIntentSnapshot
        {
            Id = snapshot.Id,
            ClientSecret = snapshot.ClientSecret,
            Status = snapshot.Status,
            Amount = snapshot.Amount,
            Currency = snapshot.Currency,
            Metadata = new Dictionary<string, string>(snapshot.Metadata, StringComparer.OrdinalIgnoreCase)
        };
    }

    private static StripeCheckoutSessionSnapshot CloneCheckout(StripeCheckoutSessionSnapshot snapshot)
    {
        return new StripeCheckoutSessionSnapshot
        {
            Id = snapshot.Id,
            Url = snapshot.Url,
            Status = snapshot.Status,
            AmountTotal = snapshot.AmountTotal,
            Currency = snapshot.Currency,
            PaymentIntentId = snapshot.PaymentIntentId,
            ExpiresAt = snapshot.ExpiresAt,
            Metadata = new Dictionary<string, string>(snapshot.Metadata, StringComparer.OrdinalIgnoreCase)
        };
    }
}

public sealed class FakeEmailService : IEmailService
{
    private readonly object _lock = new();
    private readonly List<SentEmailRecord> _sentEmails = new();

    public bool ForceFailure { get; set; }
    public string FailureMessage { get; set; } = "SMTP fake failure";

    public IReadOnlyList<SentEmailRecord> SentEmails
    {
        get
        {
            lock (_lock)
            {
                return _sentEmails.ToList();
            }
        }
    }

    public Task<EmailSendResult> SendOrderTicketsAsync(OrdineTicketDocumentDTO orderDocument, byte[] pdfBytes, string fileName, CancellationToken cancellationToken = default)
    {
        if (ForceFailure)
        {
            return Task.FromResult(new EmailSendResult
            {
                Success = false,
                ErrorMessage = FailureMessage
            });
        }

        lock (_lock)
        {
            _sentEmails.Add(new SentEmailRecord
            {
                OrderId = orderDocument.OrdineId,
                RecipientEmail = orderDocument.RecipientEmail,
                FileName = fileName,
                PdfBytes = pdfBytes,
                TicketCodes = orderDocument.Tickets.Select(t => t.CodiceBiglietto).ToList()
            });
        }

        return Task.FromResult(new EmailSendResult
        {
            Success = true,
            SentAtUtc = DateTime.UtcNow
        });
    }

    public Task<EmailSendResult> SendVoucherPurchaseAsync(string recipientEmail, string recipientName, string voucherCode, decimal importo, DateTime? scadenzaUtc, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            _sentEmails.Add(new SentEmailRecord { RecipientEmail = recipientEmail, FileName = "voucher_purchase" });
        }

        return Task.FromResult(new EmailSendResult
        {
            Success = true,
            SentAtUtc = DateTime.UtcNow
        });
    }

    public void Reset()
    {
        lock (_lock)
        {
            _sentEmails.Clear();
            ForceFailure = false;
            FailureMessage = "SMTP fake failure";
        }
    }

    public Task<EmailSendResult> SendTopupConfirmationAsync(string recipientEmail, string recipientName, decimal amount, decimal newBalance, string transactionId, CancellationToken cancellationToken = default)
    {
        lock (_lock) { _sentEmails.Add(new SentEmailRecord { RecipientEmail = recipientEmail, FileName = "topup_confirmation" }); }
        return Task.FromResult(new EmailSendResult { Success = true, SentAtUtc = DateTime.UtcNow });
    }

    public Task<EmailSendResult> SendPasswordResetAsync(string recipientEmail, string recipientName, string resetLink, CancellationToken cancellationToken = default)
    {
        lock (_lock) { _sentEmails.Add(new SentEmailRecord { RecipientEmail = recipientEmail, FileName = "password_reset" }); }
        return Task.FromResult(new EmailSendResult { Success = true, SentAtUtc = DateTime.UtcNow });
    }
}

public sealed class SentEmailRecord
{
    public int OrderId { get; set; }
    public string RecipientEmail { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public byte[] PdfBytes { get; set; } = Array.Empty<byte>();
    public List<string> TicketCodes { get; set; } = new();
}
