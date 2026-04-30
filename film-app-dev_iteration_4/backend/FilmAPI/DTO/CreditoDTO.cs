namespace FilmAPI.DTO;

public class CreditoMeDTO
{
    public int UserId { get; set; }
    public decimal SaldoAttuale { get; set; }
    public List<MovimentoCreditoDTO> Movimenti { get; set; } = new();
}

public class CreditoUserLookupDTO
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string Cognome { get; set; } = string.Empty;
    public decimal CreditoResiduo { get; set; }
}

public class CreditoTopUpRequestDTO
{
    public int UserId { get; set; }
    public decimal Importo { get; set; }
    public int? CinemaId { get; set; }
    public string? Note { get; set; }
}

public class CreditoTopUpResultDTO
{
    public CreditoUserLookupDTO Utente { get; set; } = new();
    public MovimentoCreditoDTO Movimento { get; set; } = new();
}

public class MovimentoCreditoDTO
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string UserEmail { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public decimal Importo { get; set; }
    public decimal SaldoPre { get; set; }
    public decimal SaldoPost { get; set; }
    public int? OperatoreUserId { get; set; }
    public string? OperatoreEmail { get; set; }
    public int? CinemaId { get; set; }
    public string? CinemaNome { get; set; }
    public int? OrdineId { get; set; }
    public string? CodiceOrdine { get; set; }
    public string? FilmTitolo { get; set; }
    public DateTime? ShowStartAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string? Note { get; set; }
}

public class CreateTopupSessionRequestDTO
{
    public decimal Amount { get; set; }
}

public class CreateTopupSessionResponseDTO
{
    public string StripeCheckoutSessionId { get; set; } = string.Empty;
    public string StripeCheckoutUrl { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
}

public class ReconcileTopupResponseDTO
{
    public bool Success { get; set; }
    public decimal NewBalance { get; set; }
    public string? Message { get; set; }
}
