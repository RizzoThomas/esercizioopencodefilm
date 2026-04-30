namespace FilmAPI.DTO;

public enum SeatStatus
{
    Available,
    HeldByOther,
    HeldByMe,
    Sold
}

public class SeatMapDTO
{
    public int ShowId { get; set; }
    public string FilmTitolo { get; set; } = string.Empty;
    public string CinemaNome { get; set; } = string.Empty;
    public string SalaNome { get; set; } = string.Empty;
    public DateTime StartAtUtc { get; set; }
    public decimal PrezzoBase { get; set; }
    public decimal SupplementoSala { get; set; }
    public DateTime? ScadeAtUtc { get; set; }
    public List<SeatInfoDTO> Posti { get; set; } = new();
}

public class SeatInfoDTO
{
    public int SalaPostoId { get; set; }
    public string Settore { get; set; } = string.Empty;
    public int Fila { get; set; }
    public int Numero { get; set; }
    public bool IsWheelchair { get; set; }
    public SeatStatus Stato { get; set; }
}

public class SeatHoldRequestDTO
{
    public int ShowId { get; set; }
    public List<int> SalaPostoIds { get; set; } = new();
}

public class SeatHoldResponseDTO
{
    public string HoldToken { get; set; } = string.Empty;
    public DateTime ScadeAtUtc { get; set; }
    public List<int> SalaPostoIds { get; set; } = new();
    public List<string> Conflitti { get; set; } = new();
}

public class CreateOrdineRequestDTO
{
    public string HoldToken { get; set; } = string.Empty;
    public string? IdempotencyKey { get; set; }
}

public class OrdineSummaryDTO
{
    public int Id { get; set; }
    public string CodiceOrdine { get; set; } = string.Empty;
    public int ShowId { get; set; }
    public string FilmTitolo { get; set; } = string.Empty;
    public string CinemaNome { get; set; } = string.Empty;
    public string SalaNome { get; set; } = string.Empty;
    public DateTime StartAtUtc { get; set; }
    public int NumeroBiglietti { get; set; }
    public decimal TotaleLordo { get; set; }
    public decimal ImportoCredito { get; set; }
    public decimal ImportoCarta { get; set; }
    public string? StripePaymentIntentId { get; set; }
    public string? StripeCheckoutSessionId { get; set; }
    public string Stato { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? PaidAtUtc { get; set; }
    public DateTime? CheckoutExpiresAtUtc { get; set; }
    public DateTime? CheckoutCompletedAtUtc { get; set; }
    public decimal CreditoRiservato { get; set; }
    public DateTime? TicketEmailSentAtUtc { get; set; }
    public string? TicketEmailLastError { get; set; }
    public string? LastPaymentError { get; set; }
    public List<OrdineTicketSummaryDTO> Biglietti { get; set; } = new();
}
