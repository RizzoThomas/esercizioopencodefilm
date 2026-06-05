using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FilmAPI.Model;

/// <summary>
/// Biglietto emesso dalla piattaforma CineBase per una specifica prenotazione di sala.
/// Viene usato dai servizi di checkout, validazione in sala e assistenza clienti e mappa la tabella dei biglietti.
/// </summary>
public class Biglietto
{
    /// <summary>Identificativo univoco del biglietto.</summary>
    [Key]
    public int Id { get; set; }

    /// <summary>Ordine di acquisto a cui il biglietto appartiene; chiave esterna obbligatoria.</summary>
    [Required]
    public int OrdineId { get; set; }

    /// <summary>Relazione con l'ordine che ha generato il biglietto.</summary>
    [ForeignKey(nameof(OrdineId))]
    public Ordine? Ordine { get; set; }

    /// <summary>Show per cui il biglietto è valido; chiave esterna obbligatoria.</summary>
    [Required]
    public int ShowId { get; set; }

    /// <summary>Relazione con la proiezione acquistata.</summary>
    [ForeignKey(nameof(ShowId))]
    public Show? Show { get; set; }

    /// <summary>Posto di sala assegnato al biglietto; chiave esterna obbligatoria.</summary>
    [Required]
    public int SalaPostoId { get; set; }

    /// <summary>Relazione con il posto fisico della sala.</summary>
    [ForeignKey(nameof(SalaPostoId))]
    public SalaPosto? SalaPosto { get; set; }

    /// <summary>Utente titolare del biglietto; chiave esterna obbligatoria.</summary>
    [Required]
    public int UserId { get; set; }

    /// <summary>Relazione con l'utente che possiede il biglietto.</summary>
    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }

    /// <summary>Codice biglietto leggibile dall'utente; massimo 50 caratteri.</summary>
    [Required]
    [MaxLength(50)]
    public string CodiceBiglietto { get; set; } = string.Empty;

    /// <summary>Valore barcode usato dal servizio di validazione; massimo 100 caratteri.</summary>
    [Required]
    [MaxLength(100)]
    public string BarcodeValue { get; set; } = string.Empty;

    /// <summary>Prezzo base del biglietto; valore monetario a due decimali.</summary>
    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal PrezzoBase { get; set; }

    /// <summary>Supplemento applicato al biglietto; valore monetario a due decimali.</summary>
    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal Supplemento { get; set; }

    /// <summary>Prezzo totale finale del biglietto; valore monetario a due decimali.</summary>
    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal PrezzoTotale { get; set; }

    /// <summary>Stato del biglietto nel ciclo di vita della vendita.</summary>
    [Required]
    public BigliettoState Stato { get; set; }

    /// <summary>Data/ora UTC della validazione; nulla finché il biglietto non viene usato.</summary>
    public DateTime? ValidatoAtUtc { get; set; }

    /// <summary>Utente che ha validato il biglietto; chiave esterna opzionale.</summary>
    public int? ValidatoDaUserId { get; set; }

    /// <summary>Relazione con l'utente che ha validato il biglietto.</summary>
    [ForeignKey(nameof(ValidatoDaUserId))]
    public User? ValidatoDaUser { get; set; }

    /// <summary>Cinema in cui è avvenuta la validazione; chiave esterna opzionale.</summary>
    public int? ValidatoCinemaId { get; set; }

    /// <summary>Relazione con il cinema che ha registrato la validazione.</summary>
    [ForeignKey(nameof(ValidatoCinemaId))]
    public Cinema? ValidatoCinema { get; set; }

    /// <summary>Data/ora UTC dell'annullamento; nulla se il biglietto è ancora valido.</summary>
    public DateTime? CancelledAtUtc { get; set; }

    /// <summary>Utente che ha annullato il biglietto; chiave esterna opzionale.</summary>
    public int? CancelledByUserId { get; set; }

    /// <summary>Relazione con l'utente che ha effettuato l'annullamento.</summary>
    [ForeignKey(nameof(CancelledByUserId))]
    public User? CancelledByUser { get; set; }

    /// <summary>Motivazione dell'annullamento; massimo 500 caratteri.</summary>
    [MaxLength(500)]
    public string? CancellationReason { get; set; }

    /// <summary>Ordine di rimborso associato all'annullamento; chiave esterna opzionale.</summary>
    public int? OrdineRefundId { get; set; }

    /// <summary>Relazione con il rimborso collegato al biglietto.</summary>
    [ForeignKey(nameof(OrdineRefundId))]
    public OrdineRefund? OrdineRefund { get; set; }
}
