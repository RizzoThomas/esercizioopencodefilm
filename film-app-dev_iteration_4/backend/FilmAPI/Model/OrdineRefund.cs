using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FilmAPI.Model;

/// <summary>
/// Rimborso associato a un ordine CineBase.
/// Viene creato in seguito all'annullamento di uno o più biglietti e registra l'importo restituito all'utente.
/// </summary>
public class OrdineRefund
{
    /// <summary>Identificativo univoco del rimborso.</summary>
    [Key]
    public int Id { get; set; }

    /// <summary>Ordine oggetto del rimborso; chiave esterna obbligatoria.</summary>
    [Required]
    public int OrdineId { get; set; }

    /// <summary>Relazione con l'ordine rimborsato.</summary>
    [ForeignKey(nameof(OrdineId))]
    public Ordine? Ordine { get; set; }

    /// <summary>Importo totale rimborsato; valore monetario a due decimali.</summary>
    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal Importo { get; set; }

    /// <summary>Data/ora UTC di creazione del rimborso.</summary>
    [Required]
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>Data/ora UTC di completamento del rimborso; nulla se non ancora processato.</summary>
    public DateTime? CompletedAtUtc { get; set; }

    /// <summary>Motivazione del rimborso; massimo 500 caratteri.</summary>
    [MaxLength(500)]
    public string? Motivo { get; set; }

    /// <summary>Riferimento esterno (es. Stripe Refund ID); massimo 120 caratteri.</summary>
    [MaxLength(120)]
    public string? ExternalRefundId { get; set; }

    /// <summary>Stato del rimborso.</summary>
    [Required]
    public OrdineRefundState Stato { get; set; }
}
