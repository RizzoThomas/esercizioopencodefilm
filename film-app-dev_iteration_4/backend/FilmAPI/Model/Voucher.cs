namespace FilmAPI.Model;

/// <summary>
/// Voucher valore utilizzabile nell'ecosistema CineBase.
/// È usato dai servizi promo e crediti per salvare un saldo prepagato o promozionale nel database.
/// </summary>
public class Voucher
{
    /// <summary>Identificativo univoco del voucher.</summary>
    public int Id { get; set; }
    /// <summary>Codice univoco del voucher usato dal cliente.</summary>
    public string Codice { get; set; } = string.Empty;
    /// <summary>Valore iniziale caricato sul voucher.</summary>
    public decimal ImportoIniziale { get; set; }
    /// <summary>Saldo residuo ancora spendibile.</summary>
    public decimal SaldoResiduo { get; set; }
    /// <summary>Stato testuale del voucher nel workflow business.</summary>
    public string Stato { get; set; } = "attivo";
    /// <summary>Data/ora di scadenza del voucher, se prevista.</summary>
    public DateTime? DataScadenza { get; set; }
    /// <summary>Utente a cui il voucher è associato, se nominativo.</summary>
    public int? UserId { get; set; }
    /// <summary>Relazione con l'utente titolare del voucher.</summary>
    public User? User { get; set; }
    /// <summary>Data/ora UTC di creazione del voucher.</summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
