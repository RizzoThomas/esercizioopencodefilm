using FilmAPI.Data;
using FilmAPI.DTO;
using FilmAPI.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FilmAPI.Services;

public class PagamentoService : IPagamentoService
{
    private readonly FilmDbContext _db;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IStripePaymentGateway _stripeGateway;
    private readonly ICreditoService _creditoService;
    private readonly ICheckoutService _checkoutService;
    private readonly IBigliettoService _bigliettoService;
    private readonly IPdfService _pdfService;
    private readonly IEmailService _emailService;
    private readonly ILogger<PagamentoService> _logger;

    public PagamentoService(
        FilmDbContext db,
        IServiceScopeFactory scopeFactory,
        IStripePaymentGateway stripeGateway,
        ICreditoService creditoService,
        ICheckoutService checkoutService,
        IBigliettoService bigliettoService,
        IPdfService pdfService,
        IEmailService emailService,
        ILogger<PagamentoService> logger)
    {
        _db = db;
        _scopeFactory = scopeFactory;
        _stripeGateway = stripeGateway;
        _creditoService = creditoService;
        _checkoutService = checkoutService;
        _bigliettoService = bigliettoService;
        _pdfService = pdfService;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<PayOrdineResponseDTO> PayOrdineAsync(int userId, int orderId, PayOrdineRequestDTO dto, string? idempotencyKey)
    {
        var metodo = NormalizePaymentMethod(dto.MetodoPagamento);
        var ordine = await LoadOrderAsync(orderId);

        Console.WriteLine($"[PAY] userId={userId} orderId={orderId} metodo={metodo} stato={ordine?.Stato}");

        if (ordine is null || ordine.UserId != userId)
            throw new KeyNotFoundException("Ordine non trovato.");

        if (ordine.Stato == OrdineState.Paid)
        {
            Console.WriteLine($"[PAY] Ordine gia pagato, restituisco successo.");
            _ = TrySendOrderTicketsEmailAsync(ordine.Id);
            return new PayOrdineResponseDTO
            {
                StatoPagamento = "Paid",
                Ordine = await GetOrderSummaryAsync(ordine.Id, ordine.UserId),
                StripePaymentIntentId = ordine.StripePaymentIntentId
            };
        }

        if (ordine.Stato != OrdineState.Pending && ordine.Stato != OrdineState.CheckoutInProgress)
            throw new InvalidOperationException($"L'ordine non e pagabile nello stato corrente ({ordine.Stato}).");

        var holdStates = await LoadOrderHoldStatesAsync(ordine.Id);
        ValidateHoldStatesForPendingPayment(ordine, holdStates);

        var total = CalculateTotal(ordine.Show!, holdStates.Count);
        var split = ComputePaymentSplit(metodo, total, ordine.User!.CreditoResiduo, dto.ImportoCreditoRichiesto);

        if (!string.IsNullOrWhiteSpace(ordine.StripePaymentIntentId)
            && (ordine.ImportoCarta != split.ImportoCarta || ordine.ImportoCredito != split.ImportoCredito))
        {
            throw new InvalidOperationException("Esiste gia un tentativo di pagamento carta per questo ordine con uno split diverso.");
        }

        ordine.TotaleLordo = total;
        ordine.NumeroBiglietti = holdStates.Count;
        ordine.ImportoCredito = split.ImportoCredito;
        ordine.ImportoCarta = split.ImportoCarta;
        await _db.SaveChangesAsync();

        if (split.ImportoCarta == 0)
        {
            await FinalizePaidOrderAsync(ordine.Id, null);
            _ = TrySendOrderTicketsEmailAsync(ordine.Id);
            return new PayOrdineResponseDTO
            {
                StatoPagamento = "Paid",
                RequiresCardAction = false,
                Ordine = await GetOrderSummaryAsync(ordine.Id, ordine.UserId)
            };
        }

        StripePaymentIntentSnapshot paymentIntent;
        if (string.IsNullOrWhiteSpace(ordine.StripePaymentIntentId))
        {
            paymentIntent = await _stripeGateway.CreatePaymentIntentAsync(
                new StripeCreatePaymentIntentRequest
                {
                    OrderId = ordine.Id,
                    OrderCode = ordine.CodiceOrdine,
                    UserId = ordine.UserId,
                    ShowId = ordine.ShowId,
                    Amount = split.ImportoCarta
                },
                BuildStripeIdempotencyKey(orderId, idempotencyKey));

            ordine.StripePaymentIntentId = paymentIntent.Id;
            await _db.SaveChangesAsync();
        }
        else
        {
            paymentIntent = await _stripeGateway.GetPaymentIntentAsync(ordine.StripePaymentIntentId);
        }

        if (IsSucceeded(paymentIntent.Status))
        {
            await FinalizePaidOrderAsync(ordine.Id, paymentIntent.Id);
            _ = TrySendOrderTicketsEmailAsync(ordine.Id);
            return new PayOrdineResponseDTO
            {
                StatoPagamento = "Paid",
                Ordine = await GetOrderSummaryAsync(ordine.Id, ordine.UserId),
                StripePaymentIntentId = paymentIntent.Id
            };
        }

        if (IsCanceled(paymentIntent.Status))
        {
            await ReleaseOrderHoldsAsync(ordine.Id);
            ordine.Stato = OrdineState.Cancelled;
            ordine.LastPaymentError = "Il pagamento carta risulta annullato.";
            await _db.SaveChangesAsync();

            return new PayOrdineResponseDTO
            {
                StatoPagamento = paymentIntent.Status,
                Messaggio = "Il pagamento carta risulta annullato.",
                StripePaymentIntentId = paymentIntent.Id,
                Ordine = await GetOrderSummaryAsync(ordine.Id, ordine.UserId)
            };
        }

        return new PayOrdineResponseDTO
        {
            StatoPagamento = paymentIntent.Status,
            RequiresCardAction = true,
            Messaggio = "Confermare il pagamento carta con Stripe e richiamare l'endpoint per la finalizzazione.",
            StripePaymentIntentId = paymentIntent.Id,
            StripeClientSecret = paymentIntent.ClientSecret,
            Ordine = await GetOrderSummaryAsync(ordine.Id, ordine.UserId)
        };
    }

    public async Task HandleStripeWebhookAsync(string payload, string? signatureHeader)
    {
        var stripeEvent = _stripeGateway.ParseWebhookEvent(payload, signatureHeader);

        int? orderId = null;

        if (stripeEvent.PaymentIntent != null && !string.IsNullOrEmpty(stripeEvent.PaymentIntent.Id))
        {
            orderId = TryGetOrderId(stripeEvent.PaymentIntent.Metadata);
        }

        if (stripeEvent.CheckoutSession != null && !string.IsNullOrEmpty(stripeEvent.CheckoutSession.Id))
        {
            orderId = TryGetOrderId(stripeEvent.CheckoutSession.Metadata);
        }

        if (!orderId.HasValue)
        {
            var paymentIntentId = stripeEvent.PaymentIntent?.Id;
            if (!string.IsNullOrEmpty(paymentIntentId))
            {
                var ordine = await _db.Ordini
                    .Include(o => o.User)
                    .Include(o => o.Show)
                    .FirstOrDefaultAsync(o => o.StripePaymentIntentId == paymentIntentId);

                if (ordine is not null)
                    orderId = ordine.Id;
            }
        }

        if (!orderId.HasValue && stripeEvent.CheckoutSession is not null && !string.IsNullOrWhiteSpace(stripeEvent.CheckoutSession.Id))
        {
            var ordine = await _db.Ordini
                .Include(o => o.User)
                .Include(o => o.Show)
                .FirstOrDefaultAsync(o => o.StripeCheckoutSessionId == stripeEvent.CheckoutSession.Id);

            if (ordine is not null)
                orderId = ordine.Id;
        }

        if (!orderId.HasValue)
            return;

        var ordine2 = await LoadOrderAsync(orderId.Value);
        if (ordine2 is null)
            return;

        switch (stripeEvent.EventType)
        {
            case "payment_intent.succeeded":
                await HandlePaymentIntentSucceededAsync(ordine2, stripeEvent.PaymentIntent?.Id ?? string.Empty);
                break;

            case "payment_intent.payment_failed":
                await HandlePaymentIntentFailedAsync(ordine2);
                break;

            case "payment_intent.canceled":
                await HandlePaymentIntentCanceledAsync(ordine2);
                break;

            case "checkout.session.completed":
                await HandleCheckoutSessionCompletedAsync(ordine2, stripeEvent.CheckoutSession?.PaymentIntentId);
                break;

            case "checkout.session.expired":
                await HandleCheckoutSessionExpiredAsync(ordine2);
                break;
        }
    }

    private async Task HandlePaymentIntentSucceededAsync(Ordine ordine, string paymentIntentId)
    {
        if (ordine.Stato == OrdineState.Paid)
            return;

        if (ordine.Stato != OrdineState.Pending && ordine.Stato != OrdineState.CheckoutInProgress)
            return;

        await FinalizePaidOrderAsync(ordine.Id, paymentIntentId);
        _ = TrySendOrderTicketsEmailAsync(ordine.Id);
    }

    private async Task HandlePaymentIntentFailedAsync(Ordine ordine)
    {
        if (ordine.Stato == OrdineState.CheckoutInProgress)
        {
            ordine.LastPaymentError = "Pagamento carta fallito. La sessione Stripe Checkout resta aperta fino alla scadenza per consentire un nuovo tentativo.";
            await _db.SaveChangesAsync();
            return;
        }

        if (ordine.Stato == OrdineState.Pending)
        {
            ordine.Stato = OrdineState.Failed;
            ordine.LastPaymentError = "Pagamento carta fallito.";
            await ReleaseReservedCreditIfNeededAsync(ordine);
            await ReleaseOrderHoldsAsync(ordine.Id);
            await _db.SaveChangesAsync();
        }
    }

    private async Task HandlePaymentIntentCanceledAsync(Ordine ordine)
    {
        if (ordine.Stato == OrdineState.CheckoutInProgress)
        {
            ordine.LastPaymentError = "Pagamento carta annullato. La sessione Stripe Checkout potrebbe consentire un nuovo tentativo finche non scade.";
            await _db.SaveChangesAsync();
            return;
        }

        if (ordine.Stato == OrdineState.Pending)
        {
            ordine.Stato = OrdineState.Cancelled;
            await ReleaseReservedCreditIfNeededAsync(ordine);
            await ReleaseOrderHoldsAsync(ordine.Id);
            await _db.SaveChangesAsync();
        }
    }

    private async Task HandleCheckoutSessionCompletedAsync(Ordine ordine, string? paymentIntentId)
    {
        if (ordine.Stato == OrdineState.Paid)
            return;

        if (ordine.Stato != OrdineState.CheckoutInProgress)
            return;

        await FinalizePaidOrderAsync(ordine.Id, paymentIntentId);
        _ = TrySendOrderTicketsEmailAsync(ordine.Id);
    }

    private async Task HandleCheckoutSessionExpiredAsync(Ordine ordine)
    {
        if (ordine.Stato != OrdineState.CheckoutInProgress)
            return;

        await ExpireCheckoutOrderAsync(ordine);
    }

    private async Task ReleaseOrderHoldsAsync(int orderId)
    {
        var holdStates = await _db.ShowPostiStato
            .Where(s => s.OrdineId == orderId)
            .ToListAsync();

        foreach (var holdState in holdStates)
        {
            _db.ShowPostiStato.Remove(holdState);
        }

        await _db.SaveChangesAsync();
    }

    public async Task<OrdineSummaryDTO> CancelPendingOrdineAsync(int userId, int orderId)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync();

        var ordine = await _db.Ordini
            .Include(o => o.Show!)
                .ThenInclude(s => s!.Film)
            .Include(o => o.Show!)
                .ThenInclude(s => s!.Cinema)
            .Include(o => o.Show!)
                .ThenInclude(s => s!.Sala)
            .Include(o => o.Biglietti)
                .ThenInclude(b => b.SalaPosto)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

        if (ordine is null)
            throw new KeyNotFoundException("Ordine non trovato.");

        if (ordine.Stato == OrdineState.Paid)
            throw new InvalidOperationException("L'ordine e gia stato pagato e non puo essere annullato da questa schermata.");

        if (ordine.Stato != OrdineState.Pending && ordine.Stato != OrdineState.CheckoutInProgress)
            throw new InvalidOperationException("L'ordine non e annullabile nello stato corrente.");

        if (ordine.CreditoRiservato > 0)
        {
            await ReleaseReservedCreditIfNeededAsync(ordine);
        }

        var holdStates = await _db.ShowPostiStato
            .Where(s => s.OrdineId == orderId)
            .ToListAsync();

        var now = DateTime.UtcNow;
        foreach (var holdState in holdStates)
        {
            _db.ShowPostiStato.Remove(holdState);
        }

        ordine.Stato = OrdineState.Cancelled;
        ordine.StripePaymentIntentId = null;
        ordine.StripeCheckoutSessionId = null;
        ordine.ImportoCarta = 0m;
        ordine.ImportoCredito = 0m;
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        return await GetOrderSummaryAsync(orderId, userId)
            ?? throw new KeyNotFoundException("Ordine non trovato dopo annullamento.");
    }

    private async Task FinalizePaidOrderAsync(int orderId, string? paymentIntentId)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync();

        var ordine = await _db.Ordini
            .Include(o => o.User)
            .Include(o => o.Show)
            .ThenInclude(s => s!.Sala)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (ordine is null)
            throw new KeyNotFoundException("Ordine non trovato.");

        if (ordine.Stato == OrdineState.Paid)
        {
            await transaction.CommitAsync();
            return;
        }

        if (ordine.Stato != OrdineState.Pending && ordine.Stato != OrdineState.CheckoutInProgress)
            throw new InvalidOperationException("L'ordine non puo essere finalizzato nello stato corrente.");

        var holdStates = await _db.ShowPostiStato
            .Include(s => s.SalaPosto)
            .Where(s => s.OrdineId == orderId)
            .OrderBy(s => s.SalaPostoId)
            .ToListAsync();

        ValidateHoldStatesForFinalization(ordine, holdStates);

        var now = DateTime.UtcNow;
        var total = CalculateTotal(ordine.Show!, holdStates.Count);
        ordine.TotaleLordo = total;
        ordine.NumeroBiglietti = holdStates.Count;
        ordine.StripePaymentIntentId ??= paymentIntentId;
        ordine.CheckoutCompletedAtUtc ??= ordine.Stato == OrdineState.CheckoutInProgress ? now : null;

        if (ordine.CreditoRiservato > 0)
        {
            ordine.ImportoCredito = ordine.CreditoRiservato;
            ordine.CreditoRiservato = 0m;
        }
        else if (ordine.ImportoCredito > 0)
        {
            await _creditoService.ApplyOrderDebitAsync(
                ordine.UserId,
                ordine.Id,
                ordine.ImportoCredito,
                $"Addebito credito per ordine {ordine.CodiceOrdine}");
        }

        foreach (var holdState in holdStates)
        {
            holdState.Stato = ShowPostoState.Sold;
            holdState.HoldToken = null;
            holdState.ScadeAtUtc = null;
            holdState.UpdatedAtUtc = now;
            holdState.OrdineId = ordine.Id;
        }

        ordine.Stato = OrdineState.Paid;
        ordine.PaidAtUtc ??= now;

        await _bigliettoService.EmitTicketsForOrderAsync(ordine.Id);

        await _db.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    private async Task TrySendOrderTicketsEmailAsync(int orderId)
    {
        // Use a separate scope to avoid DbContext concurrency with the main request flow.
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FilmDbContext>();
        var bigliettoService = scope.ServiceProvider.GetRequiredService<IBigliettoService>();
        var pdfService = scope.ServiceProvider.GetRequiredService<IPdfService>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

        var ordine = await db.Ordini
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (ordine is null || ordine.Stato != OrdineState.Paid || ordine.TicketEmailSentAtUtc.HasValue)
            return;

        try
        {
            var ticketDocument = await bigliettoService.GetOrderTicketDocumentAsync(orderId);
            var pdfBytes = pdfService.GenerateOrderTicketsPdf(ticketDocument);
            var fileName = $"biglietti-{ticketDocument.CodiceOrdine}.pdf";
            var result = await emailService.SendOrderTicketsAsync(ticketDocument, pdfBytes, fileName);

            if (result.Success)
            {
                ordine.TicketEmailSentAtUtc = result.SentAtUtc ?? DateTime.UtcNow;
                ordine.TicketEmailLastError = null;
            }
            else
            {
                ordine.TicketEmailLastError = TruncateEmailError(result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Invio email ticket fallito per ordine {OrderId}", orderId);
            ordine.TicketEmailLastError = TruncateEmailError(ex.Message);
        }

        await db.SaveChangesAsync();
    }

    private async Task<OrdineSummaryDTO> GetOrderSummaryAsync(int orderId, int userId)
    {
        return await _checkoutService.GetOrdineByIdAsync(orderId, userId)
            ?? throw new KeyNotFoundException("Ordine non trovato.");
    }

    private async Task<Ordine?> LoadOrderAsync(int orderId)
    {
        return await _db.Ordini
            .Include(o => o.User)
            .Include(o => o.Show)
            .ThenInclude(s => s!.Film)
            .Include(o => o.Show)
            .ThenInclude(s => s!.Cinema)
            .Include(o => o.Show)
            .ThenInclude(s => s!.Sala)
            .Include(o => o.Biglietti)
            .ThenInclude(b => b.SalaPosto)
            .FirstOrDefaultAsync(o => o.Id == orderId);
    }

    private async Task<List<ShowPostoStato>> LoadOrderHoldStatesAsync(int orderId)
    {
        return await _db.ShowPostiStato
            .Include(s => s.SalaPosto)
            .Where(s => s.OrdineId == orderId)
            .OrderBy(s => s.SalaPostoId)
            .ToListAsync();
    }

    private static string NormalizePaymentMethod(string? value)
    {
        var normalized = value?.Trim().ToLowerInvariant();
        return normalized switch
        {
            "carta" or "card" => "card",
            "credito" or "credit" => "credit",
            "misto" or "mixed" => "mixed",
            _ => throw new ArgumentException("MetodoPagamento non supportato. Usare Carta, Credito o Misto.")
        };
    }

    private static (decimal ImportoCredito, decimal ImportoCarta) ComputePaymentSplit(string metodo, decimal total, decimal availableCredit, decimal? requestedCredit)
    {
        return metodo switch
        {
            "card" => (0m, total),
            "credit" => availableCredit >= total
                ? (total, 0m)
                : throw new InvalidOperationException("Credito insufficiente per completare il pagamento."),
            "mixed" => ComputeMixedSplit(total, availableCredit, requestedCredit),
            _ => throw new ArgumentException("MetodoPagamento non supportato.")
        };
    }

    private static (decimal ImportoCredito, decimal ImportoCarta) ComputeMixedSplit(decimal total, decimal availableCredit, decimal? requestedCredit)
    {
        if (!requestedCredit.HasValue)
            throw new ArgumentException("ImportoCreditoRichiesto obbligatorio per il pagamento misto.");

        var credit = decimal.Round(requestedCredit.Value, 2, MidpointRounding.AwayFromZero);
        if (credit <= 0 || credit >= total)
            throw new ArgumentException("ImportoCreditoRichiesto deve essere maggiore di zero e inferiore al totale ordine.");

        if (availableCredit < credit)
            throw new InvalidOperationException("Credito insufficiente per completare il pagamento misto.");

        return (credit, total - credit);
    }

    private static void ValidateHoldStatesForPendingPayment(Ordine ordine, List<ShowPostoStato> holdStates)
    {
        Console.WriteLine($"[PAY] Validating {holdStates.Count} hold states for order {ordine.Id}, stato ordine={ordine.Stato}");
        if (holdStates.Count == 0)
            throw new InvalidOperationException("Ordine privo di posti in hold associati.");

        var now = DateTime.UtcNow;
        var isCheckoutInProgress = ordine.Stato == OrdineState.CheckoutInProgress;

        foreach (var holdState in holdStates)
        {
            Console.WriteLine($"[PAY] HoldState: id={holdState.Id} stato={holdState.Stato} userId={holdState.UserId} scadeAt={holdState.ScadeAtUtc} now={now}");
            if (holdState.UserId != ordine.UserId)
                throw new InvalidOperationException("I posti in hold non appartengono all'utente dell'ordine.");

            if (holdState.Stato == ShowPostoState.Sold)
                continue;

            if (holdState.Stato != ShowPostoState.Hold)
                throw new InvalidOperationException($"Uno o piu posti in hold non sono piu validi per il pagamento (stato={holdState.Stato}).");

            if (!isCheckoutInProgress && holdState.ScadeAtUtc <= now)
                throw new InvalidOperationException($"Uno o piu posti in hold non sono piu validi per il pagamento (scaduto: {holdState.ScadeAtUtc} <= {now}).");
        }
    }

    private static void ValidateHoldStatesForFinalization(Ordine ordine, List<ShowPostoStato> holdStates)
    {
        ValidateHoldStatesForPendingPayment(ordine, holdStates);
    }

    private static decimal CalculateTotal(Show show, int numberOfSeats)
    {
        return (TicketPriceNormalizer.NormalizeUnitPrice(show.PrezzoBase)
            + TicketPriceNormalizer.NormalizeUnitPrice(show.SupplementoSala)) * numberOfSeats;
    }

    private static bool IsSucceeded(string? status)
    {
        return string.Equals(status, "succeeded", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsCanceled(string? status)
    {
        return string.Equals(status, "canceled", StringComparison.OrdinalIgnoreCase);
    }

    private static int? TryGetOrderId(Dictionary<string, string> metadata)
    {
        if (metadata.TryGetValue("orderId", out var orderIdRaw) && int.TryParse(orderIdRaw, out var orderId))
            return orderId;

        return null;
    }

    private static string BuildStripeIdempotencyKey(int orderId, string? idempotencyKey)
    {
        return string.IsNullOrWhiteSpace(idempotencyKey)
            ? $"checkout-order-{orderId}"
            : $"checkout-order-{orderId}-{idempotencyKey.Trim()}";
    }

    private static string? TruncateEmailError(string? error)
    {
        if (string.IsNullOrWhiteSpace(error))
            return "Invio email non riuscito.";

        var normalized = error.Trim();
        return normalized.Length <= 1000 ? normalized : normalized[..1000];
    }

    public async Task<CreateCheckoutSessionResponseDTO> CreateCheckoutSessionAsync(int userId, int orderId, CreateCheckoutSessionRequestDTO dto, string? idempotencyKey)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync();

        var ordine = await LoadOrderAsync(orderId);
        if (ordine is null || ordine.UserId != userId)
            throw new KeyNotFoundException("Ordine non trovato.");

        if (ordine.Stato != OrdineState.Pending && ordine.Stato != OrdineState.CheckoutInProgress)
            throw new InvalidOperationException("L'ordine non e in stato Pending e non puo avviare il checkout hosted.");

        var metodo = NormalizePaymentMethod(dto.MetodoPagamento);
        if (metodo != "card" && metodo != "mixed")
            throw new ArgumentException("Il checkout hosted Stripe supporta solo Carta o Misto.");

        var holdStates = await LoadOrderHoldStatesAsync(ordine.Id);
        ValidateHoldStatesForPendingPayment(ordine, holdStates);

        var total = CalculateTotal(ordine.Show!, holdStates.Count);
        ordine.TotaleLordo = total;
        ordine.NumeroBiglietti = holdStates.Count;

        var split = ComputePaymentSplit(metodo, total, ordine.User!.CreditoResiduo, dto.ImportoCreditoRichiesto);

        var frontendBaseUrl = Environment.GetEnvironmentVariable("FRONTEND_BASE_URL") ?? "http://localhost:5001";
        var successUrl = $"{frontendBaseUrl}/esito-acquisto.html?orderId={orderId}&success=true";
        var cancelUrl = $"{frontendBaseUrl}/esito-acquisto.html?orderId={orderId}&cancelled=true";

        if (ordine.CreditoRiservato > 0)
        {
            await ReleaseReservedCreditIfNeededAsync(ordine);
        }

        ordine.ImportoCredito = split.ImportoCredito;
        ordine.ImportoCarta = split.ImportoCarta;
        ordine.CreditoRiservato = split.ImportoCredito;

        if (split.ImportoCredito > 0)
        {
            await _creditoService.ReserveOrderCreditAsync(
                ordine.UserId,
                ordine.Id,
                split.ImportoCredito,
                $"Riserva credito per checkout hosted ordine {ordine.CodiceOrdine}");
        }

        await _db.SaveChangesAsync();

        var session = await _stripeGateway.CreateCheckoutSessionAsync(
            new StripeCreateCheckoutSessionRequest
            {
                OrderId = ordine.Id,
                OrderCode = ordine.CodiceOrdine,
                UserId = ordine.UserId,
                ShowId = ordine.ShowId,
                Amount = split.ImportoCarta,
                SuccessUrl = successUrl,
                CancelUrl = cancelUrl
            },
            BuildStripeIdempotencyKey(orderId, idempotencyKey));

        ordine.StripeCheckoutSessionId = session.Id;
        ordine.StripePaymentIntentId = null;
        ordine.CheckoutExpiresAtUtc = session.ExpiresAt;
        ordine.CheckoutCompletedAtUtc = null;
        ordine.LastPaymentError = null;
        ordine.Stato = OrdineState.CheckoutInProgress;
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        return new CreateCheckoutSessionResponseDTO
        {
            StripeCheckoutSessionId = session.Id,
            StripeCheckoutUrl = session.Url,
            ImportoCarta = split.ImportoCarta,
            ImportoCredito = split.ImportoCredito,
            TotaleLordo = total,
            ExpiresAtUtc = session.ExpiresAt
        };
    }

    public async Task<CheckoutStatusDTO> GetCheckoutStatusAsync(int userId, int orderId)
    {
        var ordine = await _db.Ordini
            .Include(o => o.User)
            .Include(o => o.Show)
            .ThenInclude(s => s!.Film)
            .Include(o => o.Show)
            .ThenInclude(s => s!.Cinema)
            .Include(o => o.Show)
            .ThenInclude(s => s!.Sala)
            .Include(o => o.Biglietti)
            .ThenInclude(b => b.SalaPosto)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

        if (ordine is null)
            throw new KeyNotFoundException("Ordine non trovato.");

        if (ordine.Stato == OrdineState.CheckoutInProgress && ordine.CheckoutExpiresAtUtc <= DateTime.UtcNow)
        {
            await ExpireCheckoutOrderAsync(ordine);
            ordine = await LoadOrderAsync(orderId) ?? throw new KeyNotFoundException("Ordine non trovato.");
        }

        return MapCheckoutStatus(ordine);
    }

    public async Task<CheckoutStatusDTO> ReconcileCheckoutSessionAsync(int userId, int orderId)
    {
        var ordine = await LoadOrderAsync(orderId);
        if (ordine is null || ordine.UserId != userId)
            throw new KeyNotFoundException("Ordine non trovato.");

        if (ordine.Stato == OrdineState.Paid)
        {
            return MapCheckoutStatus(ordine);
        }

        if (ordine.Stato != OrdineState.CheckoutInProgress)
        {
            return MapCheckoutStatus(ordine);
        }

        if (string.IsNullOrWhiteSpace(ordine.StripeCheckoutSessionId))
        {
            return MapCheckoutStatus(ordine);
        }

        try
        {
            var session = await _stripeGateway.GetCheckoutSessionAsync(ordine.StripeCheckoutSessionId);

            if (session.Status == "complete" || session.Status == "paid")
            {
                await HandleCheckoutSessionCompletedAsync(ordine, session.PaymentIntentId);
            }
            else if (session.Status == "expired" || (session.Status == "open" && session.ExpiresAt < DateTime.UtcNow))
            {
                await HandleCheckoutSessionExpiredAsync(ordine);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Errore durante la riconciliazione della sessione Stripe per ordine {OrderId}", orderId);
        }

        ordine = await LoadOrderAsync(orderId);
        return MapCheckoutStatus(ordine!);
    }

    private async Task ExpireCheckoutOrderAsync(Ordine ordine)
    {
        if (ordine.Stato != OrdineState.CheckoutInProgress)
            return;

        ordine.Stato = OrdineState.Expired;
        ordine.LastPaymentError = ordine.LastPaymentError ?? "Checkout scaduto.";

        var holdStates = await _db.ShowPostiStato
            .Where(s => s.OrdineId == ordine.Id)
            .ToListAsync();

        foreach (var holdState in holdStates)
        {
            _db.ShowPostiStato.Remove(holdState);
        }

        if (ordine.CreditoRiservato > 0)
        {
            await ReleaseReservedCreditIfNeededAsync(ordine);
        }

        await _db.SaveChangesAsync();
    }

    private async Task ReleaseReservedCreditIfNeededAsync(Ordine ordine)
    {
        if (ordine.CreditoRiservato <= 0)
            return;

        await _creditoService.ReleaseReservedOrderCreditAsync(
            ordine.UserId,
            ordine.Id,
            $"Rilascio credito riservato ordine {ordine.CodiceOrdine}");

        ordine.CreditoRiservato = 0m;
    }

    private CheckoutStatusDTO MapCheckoutStatus(Ordine ordine)
    {
        return new CheckoutStatusDTO
        {
            OrdineId = ordine.Id,
            CodiceOrdine = ordine.CodiceOrdine,
            Stato = ordine.Stato.ToString(),
            StripeCheckoutSessionId = ordine.StripeCheckoutSessionId,
            CheckoutExpiresAtUtc = ordine.CheckoutExpiresAtUtc,
            CheckoutCompletedAtUtc = ordine.CheckoutCompletedAtUtc,
            LastPaymentError = ordine.LastPaymentError,
            CreditoRiservato = ordine.CreditoRiservato,
            Ordine = ordine.Show != null ? new OrdineSummaryDTO
            {
                Id = ordine.Id,
                CodiceOrdine = ordine.CodiceOrdine,
                ShowId = ordine.ShowId,
                FilmTitolo = ordine.Show.Film?.Titolo ?? string.Empty,
                CinemaNome = ordine.Show.Cinema?.Nome ?? string.Empty,
                SalaNome = ordine.Show.Sala?.Nome ?? $"Sala {ordine.Show.Sala?.NumeroProgressivo}",
                StartAtUtc = ordine.Show.StartAtUtc,
                NumeroBiglietti = ordine.NumeroBiglietti,
                TotaleLordo = TicketPriceNormalizer.NormalizeTotal(ordine.TotaleLordo, ordine.NumeroBiglietti),
                ImportoCredito = TicketPriceNormalizer.NormalizeTotal(ordine.ImportoCredito, ordine.NumeroBiglietti),
                ImportoCarta = TicketPriceNormalizer.NormalizeTotal(ordine.ImportoCarta, ordine.NumeroBiglietti),
                StripePaymentIntentId = ordine.StripePaymentIntentId,
                StripeCheckoutSessionId = ordine.StripeCheckoutSessionId,
                Stato = ordine.Stato.ToString(),
                CreatedAtUtc = ordine.CreatedAtUtc,
                PaidAtUtc = ordine.PaidAtUtc,
                CheckoutExpiresAtUtc = ordine.CheckoutExpiresAtUtc,
                CheckoutCompletedAtUtc = ordine.CheckoutCompletedAtUtc,
                CreditoRiservato = ordine.CreditoRiservato,
                TicketEmailSentAtUtc = ordine.TicketEmailSentAtUtc,
                TicketEmailLastError = ordine.TicketEmailLastError,
                LastPaymentError = ordine.LastPaymentError,
                Biglietti = ordine.Biglietti
                    .OrderBy(b => b.SalaPosto?.Settore)
                    .ThenBy(b => b.SalaPosto?.Fila)
                    .ThenBy(b => b.SalaPosto?.Numero)
                    .Select(b => new OrdineTicketSummaryDTO
                    {
                        Id = b.Id,
                        SalaPostoId = b.SalaPostoId,
                        CodiceBiglietto = b.CodiceBiglietto,
                        Settore = b.SalaPosto?.Settore ?? string.Empty,
                        Fila = b.SalaPosto?.Fila ?? 0,
                        Numero = b.SalaPosto?.Numero ?? 0,
                        PrezzoTotale = TicketPriceNormalizer.NormalizeUnitPrice(b.PrezzoTotale),
                        Stato = b.Stato.ToString(),
                        ValidatoAtUtc = b.ValidatoAtUtc
                    })
                    .ToList()
            } : null
        };
    }

    private static string TruncateError(string? error)
    {
        if (string.IsNullOrWhiteSpace(error))
            return "Errore sconosciuto.";

        var normalized = error.Trim();
        return normalized.Length <= 1000 ? normalized : normalized[..1000];
    }
}
