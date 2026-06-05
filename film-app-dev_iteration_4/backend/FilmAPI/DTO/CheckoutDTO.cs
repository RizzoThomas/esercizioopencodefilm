namespace FilmAPI.DTO;

/// <summary>Stato di un posto nel checkout; serve a rappresentare la disponibilità in mappa posti.</summary>
public enum SeatStatus
{
    Available,
    HeldByOther,
    HeldByMe,
    Sold
}

/// <summary>DTO della mappa posti usato nelle API di checkout.</summary>
public class SeatMapDTO
{
    /// <summary>ID dello show associato.</summary>
    public int ShowId { get; set; }
    /// <summary>Titolo del film.</summary>
    public string FilmTitolo { get; set; } = string.Empty;
    /// <summary>Nome del cinema.</summary>
    public string CinemaNome { get; set; } = string.Empty;
    /// <summary>Nome della sala.</summary>
    public string SalaNome { get; set; } = string.Empty;
    /// <summary>Inizio della proiezione in UTC.</summary>
    public DateTime StartAtUtc { get; set; }
    /// <summary>Prezzo base del biglietto.</summary>
    public decimal PrezzoBase { get; set; }
    /// <summary>Supplemento sala.</summary>
    public decimal SupplementoSala { get; set; }
    /// <summary>Scadenza della prenotazione posti, se presente.</summary>
    public DateTime? ScadeAtUtc { get; set; }
    /// <summary>Elenco posti mostrati nella mappa.</summary>
    public List<SeatInfoDTO> Posti { get; set; } = new();
}

public class SeatInfoDTO
{
    /// <summary>ID del posto in sala.</summary>
    public int SalaPostoId { get; set; }
    /// <summary>Settore del posto.</summary>
    public string Settore { get; set; } = string.Empty;
    /// <summary>Fila del posto.</summary>
    public int Fila { get; set; }
    /// <summary>Numero del posto.</summary>
    public int Numero { get; set; }
    /// <summary>Indica se il posto è accessibile in sedia a rotelle.</summary>
    public bool IsWheelchair { get; set; }
    /// <summary>Stato corrente del posto nella mappa.</summary>
    public SeatStatus Stato { get; set; }
}

public class SeatHoldRequestDTO
{
    /// <summary>ID dello show da bloccare.</summary>
    public int ShowId { get; set; }
    /// <summary>Lista ID posti da trattenere.</summary>
    public List<int> SalaPostoIds { get; set; } = new();
}

public class SeatHoldResponseDTO
{
    /// <summary>Token di hold restituito dal server.</summary>
    public string HoldToken { get; set; } = string.Empty;
    /// <summary>Scadenza dell'hold.</summary>
    public DateTime ScadeAtUtc { get; set; }
    /// <summary>ID dei posti confermati in hold.</summary>
    public List<int> SalaPostoIds { get; set; } = new();
    /// <summary>Conflitti trovati sui posti richiesti.</summary>
    public List<string> Conflitti { get; set; } = new();
}

public class CreateOrdineRequestDTO
{
    /// <summary>Token di hold dei posti; serve a convertire il blocco in ordine.</summary>
    public string HoldToken { get; set; } = string.Empty;
    /// <summary>Chiave di idempotenza opzionale per evitare doppie creazioni.</summary>
    public string? IdempotencyKey { get; set; }
}

public class OrdineSummaryDTO
{
    /// <summary>ID univoco dell'ordine.</summary>
    public int Id { get; set; }
    /// <summary>Codice ordine leggibile.</summary>
    public string CodiceOrdine { get; set; } = string.Empty;
    /// <summary>ID dello show.</summary>
    public int ShowId { get; set; }
    /// <summary>Titolo del film.</summary>
    public string FilmTitolo { get; set; } = string.Empty;
    /// <summary>Nome del cinema.</summary>
    public string CinemaNome { get; set; } = string.Empty;
    /// <summary>Nome della sala.</summary>
    public string SalaNome { get; set; } = string.Empty;
    /// <summary>Data/ora UTC di inizio show.</summary>
    public DateTime StartAtUtc { get; set; }
    /// <summary>Numero di biglietti acquistati.</summary>
    public int NumeroBiglietti { get; set; }
    /// <summary>Totale lordo dell'ordine.</summary>
    public decimal TotaleLordo { get; set; }
    /// <summary>Importo coperto da credito.</summary>
    public decimal ImportoCredito { get; set; }
    /// <summary>Importo coperto da carta.</summary>
    public decimal ImportoCarta { get; set; }
    /// <summary>ID intent pagamento Stripe, se presente.</summary>
    public string? StripePaymentIntentId { get; set; }
    /// <summary>ID checkout session Stripe, se presente.</summary>
    public string? StripeCheckoutSessionId { get; set; }
    /// <summary>Stato corrente dell'ordine.</summary>
    public string Stato { get; set; } = string.Empty;
    /// <summary>Data UTC di creazione.</summary>
    public DateTime CreatedAtUtc { get; set; }
    /// <summary>Data UTC di pagamento, se presente.</summary>
    public DateTime? PaidAtUtc { get; set; }
    /// <summary>Scadenza UTC del checkout, se presente.</summary>
    public DateTime? CheckoutExpiresAtUtc { get; set; }
    /// <summary>Data UTC di completamento del checkout, se presente.</summary>
    public DateTime? CheckoutCompletedAtUtc { get; set; }
    /// <summary>Credito riservato per l'ordine.</summary>
    public decimal CreditoRiservato { get; set; }
    /// <summary>Data UTC invio email biglietti, se presente.</summary>
    public DateTime? TicketEmailSentAtUtc { get; set; }
    /// <summary>Ultimo errore invio email biglietti, se presente.</summary>
    public string? TicketEmailLastError { get; set; }
    /// <summary>Ultimo errore di pagamento, se presente.</summary>
    public string? LastPaymentError { get; set; }
    /// <summary>Biglietti associati all'ordine.</summary>
    public List<OrdineTicketSummaryDTO> Biglietti { get; set; } = new();
}
