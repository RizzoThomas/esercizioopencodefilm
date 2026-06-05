using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FilmAPI.Model;

/// <summary>
/// Stato di un posto per una specifica proiezione nella piattaforma CineBase.
/// È usato dai servizi di prenotazione e vendita per memorizzare la disponibilità nel database.
/// </summary>
public class ShowPostoStato
{
    /// <summary>Identificativo univoco del record.</summary>
    [Key]
    public int Id { get; set; }

    [Required]
    /// <summary>Show a cui si riferisce lo stato; chiave esterna obbligatoria.</summary>
    public int ShowId { get; set; }

    [ForeignKey(nameof(ShowId))]
    /// <summary>Relazione con la proiezione collegata.</summary>
    public Show? Show { get; set; }

    [Required]
    /// <summary>Posto fisico della sala; chiave esterna obbligatoria.</summary>
    public int SalaPostoId { get; set; }

    [ForeignKey(nameof(SalaPostoId))]
    /// <summary>Relazione con il posto fisico.</summary>
    public SalaPosto? SalaPosto { get; set; }

    [Required]
    /// <summary>Utente che possiede o ha bloccato il posto; chiave esterna obbligatoria.</summary>
    public int UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    /// <summary>Relazione con l'utente associato allo stato.</summary>
    public User? User { get; set; }

    [Required]
    /// <summary>Stato logico del posto nella proiezione.</summary>
    public ShowPostoState Stato { get; set; }

    [MaxLength(120)]
    /// <summary>Token di hold temporaneo; massimo 120 caratteri.</summary>
    public string? HoldToken { get; set; }

    /// <summary>Data/ora UTC di scadenza dell'hold; nulla se non presente.</summary>
    public DateTime? ScadeAtUtc { get; set; }

    /// <summary>Ordine collegato al posto, se venduto; chiave esterna opzionale.</summary>
    public int? OrdineId { get; set; }

    [ForeignKey(nameof(OrdineId))]
    /// <summary>Relazione con l'ordine associato.</summary>
    public Ordine? Ordine { get; set; }

    [Required]
    /// <summary>Data/ora UTC di ultimo aggiornamento del record.</summary>
    public DateTime UpdatedAtUtc { get; set; }
}
