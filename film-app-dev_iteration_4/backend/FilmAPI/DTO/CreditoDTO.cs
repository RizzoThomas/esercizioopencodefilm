namespace FilmAPI.DTO;

/// <summary>DTO del credito personale dell'utente; usato nelle API del saldo account.</summary>
public class CreditoMeDTO
{
    /// <summary>ID dell'utente titolare del credito.</summary>
    public int UserId { get; set; }
    /// <summary>Saldo attuale disponibile.</summary>
    public decimal SaldoAttuale { get; set; }
    /// <summary>Movimenti associati al saldo; servono a spiegare il conteggio.</summary>
    public List<MovimentoCreditoDTO> Movimenti { get; set; } = new();
}

/// <summary>DTO di lookup utente per operazioni di credito da staff o admin.</summary>
public class CreditoUserLookupDTO
{
    /// <summary>ID univoco dell'utente.</summary>
    public int Id { get; set; }
    /// <summary>Email dell'utente.</summary>
    public string Email { get; set; } = string.Empty;
    /// <summary>Nome dell'utente.</summary>
    public string Nome { get; set; } = string.Empty;
    /// <summary>Cognome dell'utente.</summary>
    public string Cognome { get; set; } = string.Empty;
    /// <summary>Credito residuo disponibile.</summary>
    public decimal CreditoResiduo { get; set; }
}

/// <summary>DTO di richiesta ricarica credito; usato nelle API di top-up.</summary>
public class CreditoTopUpRequestDTO
{
    /// <summary>ID dell'utente da ricaricare.</summary>
    public int UserId { get; set; }
    /// <summary>Importo da aggiungere al credito.</summary>
    public decimal Importo { get; set; }
    /// <summary>ID cinema opzionale; serve a tracciare l'operazione su una sede.</summary>
    public int? CinemaId { get; set; }
    /// <summary>Note opzionali sulla ricarica.</summary>
    public string? Note { get; set; }
}

/// <summary>DTO di risposta dopo una ricarica credito.</summary>
public class CreditoTopUpResultDTO
{
    /// <summary>Dettaglio utente aggiornato.</summary>
    public CreditoUserLookupDTO Utente { get; set; } = new();
    /// <summary>Movimento appena creato.</summary>
    public MovimentoCreditoDTO Movimento { get; set; } = new();
}

/// <summary>DTO di un movimento credito; rappresenta una variazione di saldo.</summary>
public class MovimentoCreditoDTO
{
    /// <summary>ID del movimento.</summary>
    public int Id { get; set; }
    /// <summary>ID dell'utente proprietario del movimento.</summary>
    public int UserId { get; set; }
    /// <summary>Email dell'utente.</summary>
    public string UserEmail { get; set; } = string.Empty;
    /// <summary>Tipo di movimento; serve a distinguere la causale.</summary>
    public string Tipo { get; set; } = string.Empty;
    /// <summary>Importo del movimento.</summary>
    public decimal Importo { get; set; }
    /// <summary>Saldo prima del movimento.</summary>
    public decimal SaldoPre { get; set; }
    /// <summary>Saldo dopo il movimento.</summary>
    public decimal SaldoPost { get; set; }
    /// <summary>ID dell'operatore che ha eseguito l'azione, se presente.</summary>
    public int? OperatoreUserId { get; set; }
    /// <summary>Email dell'operatore, se presente.</summary>
    public string? OperatoreEmail { get; set; }
    /// <summary>ID del cinema associato, se presente.</summary>
    public int? CinemaId { get; set; }
    /// <summary>Nome del cinema associato, se presente.</summary>
    public string? CinemaNome { get; set; }
    /// <summary>ID dell'ordine correlato, se presente.</summary>
    public int? OrdineId { get; set; }
    /// <summary>Codice ordine correlato, se presente.</summary>
    public string? CodiceOrdine { get; set; }
    /// <summary>Titolo film correlato, se presente.</summary>
    public string? FilmTitolo { get; set; }
    /// <summary>Inizio show correlato, se presente.</summary>
    public DateTime? ShowStartAtUtc { get; set; }
    /// <summary>Data UTC di creazione del movimento.</summary>
    public DateTime CreatedAtUtc { get; set; }
    /// <summary>Note opzionali; servono per tracciare il contesto.</summary>
    public string? Note { get; set; }
}

/// <summary>DTO di apertura sessione Stripe per la ricarica credito.</summary>
public class CreateTopupSessionRequestDTO
{
    /// <summary>Importo richiesto per la ricarica.</summary>
    public decimal Amount { get; set; }
}

/// <summary>DTO di risposta per la sessione Stripe di ricarica credito.</summary>
public class CreateTopupSessionResponseDTO
{
    /// <summary>ID della checkout session Stripe.</summary>
    public string StripeCheckoutSessionId { get; set; } = string.Empty;
    /// <summary>URL della checkout session Stripe.</summary>
    public string StripeCheckoutUrl { get; set; } = string.Empty;
    /// <summary>Importo richiesto.</summary>
    public decimal Amount { get; set; }
    /// <summary>Scadenza UTC della sessione.</summary>
    public DateTime ExpiresAtUtc { get; set; }
}

/// <summary>DTO di esito riconciliazione top-up.</summary>
public class ReconcileTopupResponseDTO
{
    /// <summary>Indica se l'operazione è andata a buon fine.</summary>
    public bool Success { get; set; }
    /// <summary>Nuovo saldo dopo la riconciliazione.</summary>
    public decimal NewBalance { get; set; }
    /// <summary>Messaggio opzionale di dettaglio.</summary>
    public string? Message { get; set; }
}
