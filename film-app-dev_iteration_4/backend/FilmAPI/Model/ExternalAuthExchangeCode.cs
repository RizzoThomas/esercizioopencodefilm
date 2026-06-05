using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FilmAPI.Model;

/// <summary>
/// Codice di scambio temporaneo usato dalla piattaforma CineBase dopo un login esterno.
/// Serve ai servizi di autenticazione per convertire il completamento del social login in token applicativi e mappa la tabella dei codici one-time.
/// </summary>
public class ExternalAuthExchangeCode
{
    /// <summary>Identificativo univoco del codice di scambio.</summary>
    [Key]
    public int Id { get; set; }

    /// <summary>Utente a cui il codice è collegato; chiave esterna obbligatoria.</summary>
    public int UserId { get; set; }

    /// <summary>Relazione con l'utente che completerà il login.</summary>
    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    /// <summary>Hash SHA-256 del codice di scambio; massimo 128 caratteri.</summary>
    [Required]
    [MaxLength(128)]
    public string CodeHash { get; set; } = string.Empty;

    /// <summary>Percorso di redirect dopo il completamento; massimo 512 caratteri.</summary>
    [Required]
    [MaxLength(512)]
    public string RedirectPath { get; set; } = string.Empty;

    /// <summary>Data/ora UTC di creazione del codice.</summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>Data/ora UTC di scadenza del codice.</summary>
    public DateTime ExpiresAtUtc { get; set; }

    /// <summary>Data/ora UTC di consumo del codice; nulla finché non viene usato.</summary>
    public DateTime? ConsumedAtUtc { get; set; }

    /// <summary>Provider esterno che ha originato il flusso.</summary>
    public ExternalLoginProvider Provider { get; set; }
}
