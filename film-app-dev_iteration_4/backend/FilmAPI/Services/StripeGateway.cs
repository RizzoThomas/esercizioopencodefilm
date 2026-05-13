using Stripe;
using Stripe.Checkout;

namespace FilmAPI.Services;

public class StripeCreatePaymentIntentRequest
{
    public int OrderId { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public int UserId { get; set; }
    public int ShowId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "eur";
}

public class StripeCreateCheckoutSessionRequest
{
    public int OrderId { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public int UserId { get; set; }
    public int ShowId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "eur";
    public string SuccessUrl { get; set; } = string.Empty;
    public string CancelUrl { get; set; } = string.Empty;
}

public class StripePaymentIntentSnapshot
{
    public string Id { get; set; } = string.Empty;
    public string? ClientSecret { get; set; }
    public string Status { get; set; } = string.Empty;
    public long Amount { get; set; }
    public string Currency { get; set; } = "eur";
    public Dictionary<string, string> Metadata { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public class StripeCheckoutSessionSnapshot
{
    public string Id { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public long AmountTotal { get; set; }
    public string Currency { get; set; } = "eur";
    public string? PaymentIntentId { get; set; }
    public DateTime ExpiresAt { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public class StripeWebhookEvent
{
    public string EventId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public StripePaymentIntentSnapshot PaymentIntent { get; set; } = new();
    public StripeCheckoutSessionSnapshot? CheckoutSession { get; set; }
}

public interface IStripePaymentGateway
{
    Task<StripePaymentIntentSnapshot> CreatePaymentIntentAsync(StripeCreatePaymentIntentRequest request, string? idempotencyKey, CancellationToken cancellationToken = default);
    Task<StripePaymentIntentSnapshot> GetPaymentIntentAsync(string paymentIntentId, CancellationToken cancellationToken = default);
    Task<StripeCheckoutSessionSnapshot> CreateCheckoutSessionAsync(StripeCreateCheckoutSessionRequest request, string? idempotencyKey, CancellationToken cancellationToken = default);
    Task<StripeCheckoutSessionSnapshot> GetCheckoutSessionAsync(string sessionId, CancellationToken cancellationToken = default);
    StripeWebhookEvent ParseWebhookEvent(string payload, string? signatureHeader);
}

public class StripePaymentGateway : IStripePaymentGateway
{
    private readonly StripeClient _stripeClient;
    private readonly string _webhookSecret;

    public StripePaymentGateway()
    {
        var apiKey = Environment.GetEnvironmentVariable("STRIPE_SECRET_API_KEY")
            ?? Environment.GetEnvironmentVariable("STRIPE_API_KEY");

        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("Configurazione Stripe mancante: STRIPE_SECRET_API_KEY o STRIPE_API_KEY.");

        _webhookSecret = Environment.GetEnvironmentVariable("STRIPE_WEBHOOK_SECRET") ?? string.Empty;
        _stripeClient = new StripeClient(apiKey);
    }

    public async Task<StripePaymentIntentSnapshot> CreatePaymentIntentAsync(StripeCreatePaymentIntentRequest request, string? idempotencyKey, CancellationToken cancellationToken = default)
    {
        var options = new PaymentIntentCreateOptions
        {
            Amount = ToStripeAmount(request.Amount),
            Currency = request.Currency,
            PaymentMethodTypes = new List<string> { "card" },
            Description = $"Ordine CineBase {request.OrderCode}",
            Metadata = new Dictionary<string, string>
            {
                ["orderId"] = request.OrderId.ToString(),
                ["orderCode"] = request.OrderCode,
                ["userId"] = request.UserId.ToString(),
                ["showId"] = request.ShowId.ToString()
            }
        };

        var service = new PaymentIntentService(_stripeClient);
        var paymentIntent = await service.CreateAsync(
            options,
            new RequestOptions { IdempotencyKey = string.IsNullOrWhiteSpace(idempotencyKey) ? null : idempotencyKey },
            cancellationToken);

        return MapSnapshot(paymentIntent);
    }

    public async Task<StripePaymentIntentSnapshot> GetPaymentIntentAsync(string paymentIntentId, CancellationToken cancellationToken = default)
    {
        var service = new PaymentIntentService(_stripeClient);
        var paymentIntent = await service.GetAsync(paymentIntentId, null, null, cancellationToken);
        return MapSnapshot(paymentIntent);
    }

    public StripeWebhookEvent ParseWebhookEvent(string payload, string? signatureHeader)
    {
        if (string.IsNullOrWhiteSpace(_webhookSecret))
            throw new InvalidOperationException("Configurazione Stripe mancante: STRIPE_WEBHOOK_SECRET.");

        if (string.IsNullOrWhiteSpace(signatureHeader))
            throw new InvalidOperationException("Header Stripe-Signature mancante.");

        var stripeEvent = EventUtility.ConstructEvent(
            payload,
            signatureHeader,
            _webhookSecret,
            throwOnApiVersionMismatch: false);

        var result = new StripeWebhookEvent
        {
            EventId = stripeEvent.Id,
            EventType = stripeEvent.Type
        };

        if (stripeEvent.Data.Object is PaymentIntent paymentIntent)
        {
            result.PaymentIntent = MapSnapshot(paymentIntent);
        }
        else if (stripeEvent.Data.Object is Session checkoutSession)
        {
            result.CheckoutSession = MapCheckoutSnapshot(checkoutSession);
            if (!string.IsNullOrEmpty(checkoutSession.PaymentIntentId))
            {
                result.PaymentIntent = new StripePaymentIntentSnapshot { Id = checkoutSession.PaymentIntentId };
            }
        }

        return result;
    }

    private static StripePaymentIntentSnapshot MapSnapshot(PaymentIntent paymentIntent)
    {
        var metadata = paymentIntent.Metadata ?? new Dictionary<string, string>();

        return new StripePaymentIntentSnapshot
        {
            Id = paymentIntent.Id,
            ClientSecret = paymentIntent.ClientSecret,
            Status = paymentIntent.Status,
            Amount = paymentIntent.Amount,
            Currency = paymentIntent.Currency,
            Metadata = new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase)
        };
    }

    public async Task<StripeCheckoutSessionSnapshot> CreateCheckoutSessionAsync(StripeCreateCheckoutSessionRequest request, string? idempotencyKey, CancellationToken cancellationToken = default)
    {
        var options = new SessionCreateOptions
        {
            Mode = "payment",
            PaymentMethodTypes = new List<string> { "card" },
            SuccessUrl = request.SuccessUrl,
            CancelUrl = request.CancelUrl,
            LineItems = new List<SessionLineItemOptions>
            {
                new()
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        UnitAmount = ToStripeAmount(request.Amount),
                        Currency = request.Currency,
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = request.OrderId == 0
                                ? $"Ricarica Credito CineBase €{request.Amount:F2}"
                                : $"Ordine CineBase {request.OrderCode}"
                        }
                    },
                    Quantity = 1
                }
            },
            Metadata = new Dictionary<string, string>
            {
                ["orderId"] = request.OrderId.ToString(),
                ["orderCode"] = request.OrderCode,
                ["userId"] = request.UserId.ToString(),
                ["showId"] = request.ShowId.ToString(),
                ["topupAmount"] = request.Amount.ToString("F2")
            },
            PaymentIntentData = new SessionPaymentIntentDataOptions
            {
                Metadata = new Dictionary<string, string>
                {
                    ["orderId"] = request.OrderId.ToString(),
                    ["orderCode"] = request.OrderCode,
                    ["userId"] = request.UserId.ToString(),
                    ["showId"] = request.ShowId.ToString(),
                    ["topupAmount"] = request.Amount.ToString("F2")
                }
            }
        };

        var service = new SessionService(_stripeClient);
        var session = await service.CreateAsync(
            options,
            new RequestOptions { IdempotencyKey = string.IsNullOrWhiteSpace(idempotencyKey) ? null : idempotencyKey },
            cancellationToken);

        return MapCheckoutSnapshot(session);
    }

    public async Task<StripeCheckoutSessionSnapshot> GetCheckoutSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        var service = new SessionService(_stripeClient);
        var session = await service.GetAsync(sessionId, null, null, cancellationToken);
        return MapCheckoutSnapshot(session);
    }

    private static StripeCheckoutSessionSnapshot MapCheckoutSnapshot(Session session)
    {
        var metadata = session.Metadata ?? new Dictionary<string, string>();

        return new StripeCheckoutSessionSnapshot
        {
            Id = session.Id,
            Url = session.Url ?? string.Empty,
            Status = session.Status ?? string.Empty,
            AmountTotal = session.AmountTotal ?? 0,
            Currency = session.Currency ?? "eur",
            PaymentIntentId = session.PaymentIntentId,
            ExpiresAt = session.ExpiresAt,
            Metadata = new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase)
        };
    }

    private static long ToStripeAmount(decimal amount)
    {
        return checked((long)decimal.Round(amount * 100m, 0, MidpointRounding.AwayFromZero));
    }
}
