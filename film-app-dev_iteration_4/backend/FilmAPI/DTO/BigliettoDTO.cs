namespace FilmAPI.DTO;

/// <summary>DTO sintetico del biglietto associato a un ordine.</summary>
public class OrdineTicketSummaryDTO
{
    /// <summary>ID univoco del ticket.</summary>
    public int Id { get; set; }
    /// <summary>ID del posto in sala.</summary>
    public int SalaPostoId { get; set; }
    /// <summary>Codice del biglietto.</summary>
    public string CodiceBiglietto { get; set; } = string.Empty;
    /// <summary>Settore del posto.</summary>
    public string Settore { get; set; } = string.Empty;
    /// <summary>Fila del posto.</summary>
    public int Fila { get; set; }
    /// <summary>Numero del posto.</summary>
    public int Numero { get; set; }
    /// <summary>Prezzo totale del ticket.</summary>
    public decimal PrezzoTotale { get; set; }
    /// <summary>Stato del ticket.</summary>
    public string Stato { get; set; } = string.Empty;
    /// <summary>Data di validazione, se presente.</summary>
    public DateTime? ValidatoAtUtc { get; set; }
}

/// <summary>DTO sintetico del biglietto usato nelle liste e negli ordini.</summary>
public class BigliettoSummaryDTO
{
    /// <summary>ID univoco del biglietto.</summary>
    public int Id { get; set; }
    /// <summary>ID dell'ordine associato.</summary>
    public int OrdineId { get; set; }
    /// <summary>ID dello show associato.</summary>
    public int ShowId { get; set; }
    /// <summary>Codice del biglietto.</summary>
    public string CodiceBiglietto { get; set; } = string.Empty;
    /// <summary>Titolo del film.</summary>
    public string FilmTitolo { get; set; } = string.Empty;
    /// <summary>Nome del cinema.</summary>
    public string CinemaNome { get; set; } = string.Empty;
    /// <summary>Nome della sala.</summary>
    public string SalaNome { get; set; } = string.Empty;
    /// <summary>Inizio show in UTC.</summary>
    public DateTime StartAtUtc { get; set; }
    /// <summary>Settore del posto.</summary>
    public string Settore { get; set; } = string.Empty;
    /// <summary>Fila del posto.</summary>
    public int Fila { get; set; }
    /// <summary>Numero del posto.</summary>
    public int Numero { get; set; }
    /// <summary>Prezzo totale del ticket.</summary>
    public decimal PrezzoTotale { get; set; }
    /// <summary>Stato del ticket.</summary>
    public string Stato { get; set; } = string.Empty;
    /// <summary>Data di validazione, se presente.</summary>
    public DateTime? ValidatoAtUtc { get; set; }
}

/// <summary>DTO di dettaglio del biglietto; estende il riepilogo con dati di scansione e prezzo.</summary>
public class BigliettoDetailDTO : BigliettoSummaryDTO
{
    /// <summary>Valore barcode del biglietto.</summary>
    public string BarcodeValue { get; set; } = string.Empty;
    /// <summary>Prezzo base.</summary>
    public decimal PrezzoBase { get; set; }
    /// <summary>Supplemento applicato.</summary>
    public decimal Supplemento { get; set; }
    /// <summary>ID dell'utente che ha validato il ticket, se presente.</summary>
    public int? ValidatoDaUserId { get; set; }
    /// <summary>ID del cinema dove è avvenuta la validazione, se presente.</summary>
    public int? ValidatoCinemaId { get; set; }
}

/// <summary>DTO di richiesta validazione ticket.</summary>
public class TicketValidationRequestDTO
{
    /// <summary>Codice biglietto da validare.</summary>
    public string CodiceBiglietto { get; set; } = string.Empty;
    /// <summary>ID del cinema che effettua la validazione.</summary>
    public int CinemaId { get; set; }
}

/// <summary>DTO di lookup del ticket per validazione e controlli staff.</summary>
public class TicketValidationLookupDTO
{
    /// <summary>ID univoco del ticket.</summary>
    public int TicketId { get; set; }
    /// <summary>ID dell'ordine associato.</summary>
    public int OrdineId { get; set; }
    /// <summary>ID dello show.</summary>
    public int ShowId { get; set; }
    /// <summary>ID del cinema.</summary>
    public int CinemaId { get; set; }
    /// <summary>Codice del biglietto.</summary>
    public string CodiceBiglietto { get; set; } = string.Empty;
    /// <summary>Valore barcode.</summary>
    public string BarcodeValue { get; set; } = string.Empty;
    /// <summary>Titolo del film.</summary>
    public string FilmTitolo { get; set; } = string.Empty;
    /// <summary>Nome del cinema.</summary>
    public string CinemaNome { get; set; } = string.Empty;
    /// <summary>Città del cinema.</summary>
    public string CinemaCitta { get; set; } = string.Empty;
    /// <summary>Indirizzo del cinema.</summary>
    public string CinemaIndirizzo { get; set; } = string.Empty;
    /// <summary>Codice locale del cinema, se presente.</summary>
    public string? CinemaCodiceLocale { get; set; }
    /// <summary>Nome della sala.</summary>
    public string SalaNome { get; set; } = string.Empty;
    /// <summary>Inizio show in UTC.</summary>
    public DateTime StartAtUtc { get; set; }
    /// <summary>Settore del posto.</summary>
    public string Settore { get; set; } = string.Empty;
    /// <summary>Fila del posto.</summary>
    public int Fila { get; set; }
    /// <summary>Numero del posto.</summary>
    public int Numero { get; set; }
    /// <summary>Prezzo base del ticket.</summary>
    public decimal PrezzoBase { get; set; }
    /// <summary>Supplemento applicato.</summary>
    public decimal Supplemento { get; set; }
    /// <summary>Prezzo totale del ticket.</summary>
    public decimal PrezzoTotale { get; set; }
    /// <summary>Stato del ticket.</summary>
    public string Stato { get; set; } = string.Empty;
    /// <summary>Data di validazione, se presente.</summary>
    public DateTime? ValidatoAtUtc { get; set; }
    /// <summary>ID dell'utente che ha validato, se presente.</summary>
    public int? ValidatoDaUserId { get; set; }
    /// <summary>ID del cinema che ha validato, se presente.</summary>
    public int? ValidatoCinemaId { get; set; }
}

/// <summary>DTO di esito validazione ticket.</summary>
public class TicketValidationResultDTO
{
    /// <summary>Indica se la validazione è riuscita.</summary>
    public bool Success { get; set; }
    /// <summary>Messaggio di esito.</summary>
    public string Message { get; set; } = string.Empty;
    /// <summary>Dettaglio del ticket validato o trovato.</summary>
    public TicketValidationLookupDTO Ticket { get; set; } = new();
}

/// <summary>Modello dati per generare il PDF del ticket.</summary>
public class TicketPdfModel
{
    /// <summary>ID del ticket.</summary>
    public int TicketId { get; set; }
    /// <summary>ID dell'ordine.</summary>
    public int OrdineId { get; set; }
    /// <summary>Codice ordine.</summary>
    public string CodiceOrdine { get; set; } = string.Empty;
    /// <summary>Codice biglietto.</summary>
    public string CodiceBiglietto { get; set; } = string.Empty;
    /// <summary>Valore barcode per la scansione.</summary>
    public string BarcodeValue { get; set; } = string.Empty;
    /// <summary>URL di validazione.</summary>
    public string ValidationUrl { get; set; } = string.Empty;
    /// <summary>Titolo del film.</summary>
    public string FilmTitolo { get; set; } = string.Empty;
    /// <summary>Inizio show in UTC.</summary>
    public DateTime StartAtUtc { get; set; }
    /// <summary>Nome del cinema.</summary>
    public string CinemaNome { get; set; } = string.Empty;
    /// <summary>Città del cinema.</summary>
    public string CinemaCitta { get; set; } = string.Empty;
    /// <summary>Indirizzo del cinema.</summary>
    public string CinemaIndirizzo { get; set; } = string.Empty;
    /// <summary>Codice locale del cinema, se presente.</summary>
    public string? CinemaCodiceLocale { get; set; }
    /// <summary>Nome della sala.</summary>
    public string SalaNome { get; set; } = string.Empty;
    /// <summary>Numero progressivo della sala.</summary>
    public int SalaNumeroProgressivo { get; set; }
    /// <summary>Settore del posto.</summary>
    public string Settore { get; set; } = string.Empty;
    /// <summary>Fila del posto.</summary>
    public int Fila { get; set; }
    /// <summary>Numero del posto.</summary>
    public int Numero { get; set; }
    /// <summary>Prezzo base.</summary>
    public decimal PrezzoBase { get; set; }
    /// <summary>Supplemento applicato.</summary>
    public decimal Supplemento { get; set; }
    /// <summary>Prezzo totale.</summary>
    public decimal PrezzoTotale { get; set; }
}

/// <summary>Documento ordine-ticket usato per email e PDF.</summary>
public class OrdineTicketDocumentDTO
{
    /// <summary>ID dell'ordine.</summary>
    public int OrdineId { get; set; }
    /// <summary>Codice ordine.</summary>
    public string CodiceOrdine { get; set; } = string.Empty;
    /// <summary>ID dell'utente.</summary>
    public int UserId { get; set; }
    /// <summary>Email destinataria del documento.</summary>
    public string RecipientEmail { get; set; } = string.Empty;
    /// <summary>Nome destinatario.</summary>
    public string RecipientName { get; set; } = string.Empty;
    /// <summary>Titolo del film.</summary>
    public string FilmTitolo { get; set; } = string.Empty;
    /// <summary>Nome del cinema.</summary>
    public string CinemaNome { get; set; } = string.Empty;
    /// <summary>Nome della sala.</summary>
    public string SalaNome { get; set; } = string.Empty;
    /// <summary>Inizio show in UTC.</summary>
    public DateTime StartAtUtc { get; set; }
    /// <summary>Numero di biglietti.</summary>
    public int NumeroBiglietti { get; set; }
    /// <summary>Totale lordo.</summary>
    public decimal TotaleLordo { get; set; }
    /// <summary>Data di pagamento, se presente.</summary>
    public DateTime? PaidAtUtc { get; set; }
    /// <summary>Lista dei ticket inclusi nel documento.</summary>
    public List<TicketPdfModel> Tickets { get; set; } = new();
}
