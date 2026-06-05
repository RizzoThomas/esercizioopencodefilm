using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FilmAPI.Model;

/// <summary>
/// Notifica mostrata all'utente nella piattaforma CineBase.
/// È usata dai servizi di comunicazione per informare su biglietti, rimborsi, offerte e promemoria e mappa la tabella delle notifiche.
/// </summary>
public class Notifica
{
    /// <summary>Identificativo univoco della notifica.</summary>
    [Key]
    public int Id { get; set; }

    /// <summary>Utente destinatario della notifica; chiave esterna obbligatoria.</summary>
    public int UserId { get; set; }

    /// <summary>Categoria funzionale della notifica, ad esempio biglietto o rimborso; massimo 50 caratteri.</summary>
    [Required]
    [MaxLength(50)]
    public string Tipo { get; set; } = string.Empty;

    /// <summary>Titolo breve visualizzato all'utente; massimo 200 caratteri.</summary>
    [Required]
    [MaxLength(200)]
    public string Titolo { get; set; } = string.Empty;

    /// <summary>Testo descrittivo opzionale della notifica; massimo 500 caratteri.</summary>
    [MaxLength(500)]
    public string? Descrizione { get; set; }

    /// <summary>Nome dell'icona da mostrare nella UI; massimo 100 caratteri.</summary>
    [MaxLength(100)]
    public string? Icona { get; set; }

    /// <summary>Indica se la notifica è già stata letta.</summary>
    public bool Letto { get; set; } = false;

    /// <summary>Data/ora UTC di creazione della notifica.</summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>Relazione con l'utente destinatario della notifica.</summary>
    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;
}
