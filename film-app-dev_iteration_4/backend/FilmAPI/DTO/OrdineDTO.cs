namespace FilmAPI.DTO;

public class PayOrdineRequestDTO
{
    public string MetodoPagamento { get; set; } = string.Empty;
    public decimal? ImportoCreditoRichiesto { get; set; }
    public string? IdempotencyKey { get; set; }
    public string? CodiceVoucher { get; init; }
    public int? OffertaId { get; init; }
}

public class PayOrdineResponseDTO
{
    public string StatoPagamento { get; set; } = string.Empty;
    public bool RequiresCardAction { get; set; }
    public string? Messaggio { get; set; }
    public string? StripePaymentIntentId { get; set; }
    public string? StripeClientSecret { get; set; }
    public OrdineSummaryDTO Ordine { get; set; } = new();
}

public class CreateCheckoutSessionRequestDTO
{
    public string MetodoPagamento { get; set; } = string.Empty;
    public decimal? ImportoCreditoRichiesto { get; set; }
    public string? IdempotencyKey { get; set; }
}

public class CreateCheckoutSessionResponseDTO
{
    public string StripeCheckoutSessionId { get; set; } = string.Empty;
    public string StripeCheckoutUrl { get; set; } = string.Empty;
    public decimal ImportoCarta { get; set; }
    public decimal ImportoCredito { get; set; }
    public decimal TotaleLordo { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
}

public class CheckoutStatusDTO
{
    public int OrdineId { get; set; }
    public string CodiceOrdine { get; set; } = string.Empty;
    public string Stato { get; set; } = string.Empty;
    public string? StripeCheckoutSessionId { get; set; }
    public DateTime? CheckoutExpiresAtUtc { get; set; }
    public DateTime? CheckoutCompletedAtUtc { get; set; }
    public string? LastPaymentError { get; set; }
    public decimal CreditoRiservato { get; set; }
    public OrdineSummaryDTO? Ordine { get; set; }
}
