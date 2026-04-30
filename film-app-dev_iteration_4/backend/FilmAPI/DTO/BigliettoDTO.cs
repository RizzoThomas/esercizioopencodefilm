namespace FilmAPI.DTO;

public class OrdineTicketSummaryDTO
{
    public int Id { get; set; }
    public int SalaPostoId { get; set; }
    public string CodiceBiglietto { get; set; } = string.Empty;
    public string Settore { get; set; } = string.Empty;
    public int Fila { get; set; }
    public int Numero { get; set; }
    public decimal PrezzoTotale { get; set; }
    public string Stato { get; set; } = string.Empty;
    public DateTime? ValidatoAtUtc { get; set; }
}

public class BigliettoSummaryDTO
{
    public int Id { get; set; }
    public int OrdineId { get; set; }
    public int ShowId { get; set; }
    public string CodiceBiglietto { get; set; } = string.Empty;
    public string FilmTitolo { get; set; } = string.Empty;
    public string CinemaNome { get; set; } = string.Empty;
    public string SalaNome { get; set; } = string.Empty;
    public DateTime StartAtUtc { get; set; }
    public string Settore { get; set; } = string.Empty;
    public int Fila { get; set; }
    public int Numero { get; set; }
    public decimal PrezzoTotale { get; set; }
    public string Stato { get; set; } = string.Empty;
    public DateTime? ValidatoAtUtc { get; set; }
}

public class BigliettoDetailDTO : BigliettoSummaryDTO
{
    public string BarcodeValue { get; set; } = string.Empty;
    public decimal PrezzoBase { get; set; }
    public decimal Supplemento { get; set; }
    public int? ValidatoDaUserId { get; set; }
    public int? ValidatoCinemaId { get; set; }
}

public class TicketValidationRequestDTO
{
    public string CodiceBiglietto { get; set; } = string.Empty;
    public int CinemaId { get; set; }
}

public class TicketValidationLookupDTO
{
    public int TicketId { get; set; }
    public int OrdineId { get; set; }
    public int ShowId { get; set; }
    public int CinemaId { get; set; }
    public string CodiceBiglietto { get; set; } = string.Empty;
    public string BarcodeValue { get; set; } = string.Empty;
    public string FilmTitolo { get; set; } = string.Empty;
    public string CinemaNome { get; set; } = string.Empty;
    public string CinemaCitta { get; set; } = string.Empty;
    public string CinemaIndirizzo { get; set; } = string.Empty;
    public string? CinemaCodiceLocale { get; set; }
    public string SalaNome { get; set; } = string.Empty;
    public DateTime StartAtUtc { get; set; }
    public string Settore { get; set; } = string.Empty;
    public int Fila { get; set; }
    public int Numero { get; set; }
    public decimal PrezzoBase { get; set; }
    public decimal Supplemento { get; set; }
    public decimal PrezzoTotale { get; set; }
    public string Stato { get; set; } = string.Empty;
    public DateTime? ValidatoAtUtc { get; set; }
    public int? ValidatoDaUserId { get; set; }
    public int? ValidatoCinemaId { get; set; }
}

public class TicketValidationResultDTO
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public TicketValidationLookupDTO Ticket { get; set; } = new();
}

public class TicketPdfModel
{
    public int TicketId { get; set; }
    public int OrdineId { get; set; }
    public string CodiceOrdine { get; set; } = string.Empty;
    public string CodiceBiglietto { get; set; } = string.Empty;
    public string BarcodeValue { get; set; } = string.Empty;
    public string ValidationUrl { get; set; } = string.Empty;
    public string FilmTitolo { get; set; } = string.Empty;
    public DateTime StartAtUtc { get; set; }
    public string CinemaNome { get; set; } = string.Empty;
    public string CinemaCitta { get; set; } = string.Empty;
    public string CinemaIndirizzo { get; set; } = string.Empty;
    public string? CinemaCodiceLocale { get; set; }
    public string SalaNome { get; set; } = string.Empty;
    public int SalaNumeroProgressivo { get; set; }
    public string Settore { get; set; } = string.Empty;
    public int Fila { get; set; }
    public int Numero { get; set; }
    public decimal PrezzoBase { get; set; }
    public decimal Supplemento { get; set; }
    public decimal PrezzoTotale { get; set; }
}

public class OrdineTicketDocumentDTO
{
    public int OrdineId { get; set; }
    public string CodiceOrdine { get; set; } = string.Empty;
    public int UserId { get; set; }
    public string RecipientEmail { get; set; } = string.Empty;
    public string RecipientName { get; set; } = string.Empty;
    public string FilmTitolo { get; set; } = string.Empty;
    public string CinemaNome { get; set; } = string.Empty;
    public string SalaNome { get; set; } = string.Empty;
    public DateTime StartAtUtc { get; set; }
    public int NumeroBiglietti { get; set; }
    public decimal TotaleLordo { get; set; }
    public DateTime? PaidAtUtc { get; set; }
    public List<TicketPdfModel> Tickets { get; set; } = new();
}
