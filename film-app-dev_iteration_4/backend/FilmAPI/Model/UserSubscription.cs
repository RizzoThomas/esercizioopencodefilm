namespace FilmAPI.Model;

/// <summary>
/// Sottoscrizione attiva di un utente a un abbonamento CineBase.
/// È usata dai servizi subscription per gestire rinnovi, pagamenti e validità nel database.
/// </summary>
public class UserSubscription
{
    /// <summary>Identificativo univoco della sottoscrizione.</summary>
    public int Id { get; set; }
    /// <summary>Utente sottoscrittore; chiave esterna obbligatoria.</summary>
    public int UserId { get; set; }
    /// <summary>Relazione con l'utente sottoscrittore.</summary>
    public User User { get; set; } = null!;
    /// <summary>Abbonamento scelto; chiave esterna obbligatoria.</summary>
    public int AbbonamentoId { get; set; }
    /// <summary>Relazione con il piano di abbonamento.</summary>
    public Abbonamento Abbonamento { get; set; } = null!;
    /// <summary>Metodo di pagamento usato per la sottoscrizione; valore testuale business.</summary>
    public string MetodoPagamento { get; set; } = "carta";
    /// <summary>Indica se il rinnovo automatico è abilitato.</summary>
    public bool AutoRinnovo { get; set; } = true;
    /// <summary>Data/ora di inizio validità della sottoscrizione.</summary>
    public DateTime DataInizio { get; set; }
    /// <summary>Data/ora di scadenza della sottoscrizione.</summary>
    public DateTime DataScadenza { get; set; }
    /// <summary>Stato corrente della sottoscrizione.</summary>
    public string Stato { get; set; } = "attivo";
    /// <summary>Data/ora UTC di creazione del record.</summary>
    public DateTime CreatedAtUtc { get; set; }
}
