namespace FilmAPI.DTO;

/// <summary>DTO di richiesta chat usato nelle API di assistenza.</summary>
public class ChatRequestDTO
{
    /// <summary>Messaggio dell'utente; rappresenta il prompt da elaborare.</summary>
    public string Message { get; set; } = string.Empty;
    /// <summary>Numero di tentativi falliti; serve per gestire escalation o ticket.</summary>
    public int FailedAttempts { get; set; } = 0;
}

/// <summary>DTO di risposta chat usato dalle API di assistenza.</summary>
public class ChatResponseDTO
{
    /// <summary>Risposta del sistema o dell'assistente.</summary>
    public string Reply { get; set; } = string.Empty;
    /// <summary>Indica se il problema risulta risolto.</summary>
    public bool IsResolved { get; set; } = true;
    /// <summary>Indica se mostrare il pulsante per aprire un ticket.</summary>
    public bool ShowTicketButton { get; set; } = false;
}

/// <summary>DTO di creazione ticket di supporto.</summary>
public class CreateTicketDTO
{
    /// <summary>Oggetto del ticket; sintetizza il problema.</summary>
    public string Oggetto { get; set; } = string.Empty;
    /// <summary>Messaggio dettagliato del ticket.</summary>
    public string Messaggio { get; set; } = string.Empty;
    /// <summary>Email di contatto opzionale.</summary>
    public string? EmailContatto { get; set; }
}

/// <summary>DTO di un ticket di supporto mostrato in elenco o dettaglio.</summary>
public class SupportTicketDTO
{
    /// <summary>ID univoco del ticket.</summary>
    public int Id { get; set; }
    /// <summary>ID utente, se il ticket è associato a un account.</summary>
    public int? UserId { get; set; }
    /// <summary>Nome utente associato, se disponibile.</summary>
    public string? NomeUtente { get; set; }
    /// <summary>Email utente associata, se disponibile.</summary>
    public string? EmailUtente { get; set; }
    /// <summary>Oggetto del ticket.</summary>
    public string Oggetto { get; set; } = string.Empty;
    /// <summary>Messaggio del ticket.</summary>
    public string Messaggio { get; set; } = string.Empty;
    /// <summary>Email di contatto opzionale.</summary>
    public string? EmailContatto { get; set; }
    /// <summary>Stato corrente del ticket.</summary>
    public string Stato { get; set; } = string.Empty;
    /// <summary>Data di creazione.</summary>
    public DateTime CreatoIl { get; set; }
    /// <summary>Data di risoluzione, se presente.</summary>
    public DateTime? RisoltoIl { get; set; }
}
