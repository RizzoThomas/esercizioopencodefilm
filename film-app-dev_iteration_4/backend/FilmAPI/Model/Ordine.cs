using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FilmAPI.Model;

/// <summary>
/// Ordine di acquisto della piattaforma CineBase.
/// È usato dai servizi checkout, ticketing, pagamento e rimborso e mappa la tabella ordini nel database.
/// </summary>
public class Ordine
{
    /// <summary>Identificativo univoco dell'ordine.</summary>
    [Key]
    public int Id { get; set; }

    /// <summary>Codice leggibile dell'ordine; massimo 50 caratteri.</summary>
    [Required]
    [MaxLength(50)]
    public string CodiceOrdine { get; set; } = string.Empty;

    /// <summary>Utente che ha creato l'ordine; chiave esterna obbligatoria.</summary>
    [Required]
    public int UserId { get; set; }

    /// <summary>Relazione con l'utente che ha effettuato l'acquisto.</summary>
    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }

    /// <summary>Show acquistato; chiave esterna obbligatoria.</summary>
    [Required]
    public int ShowId { get; set; }

    /// <summary>Relazione con la proiezione acquistata.</summary>
    [ForeignKey(nameof(ShowId))]
    public Show? Show { get; set; }

    /// <summary>Cinema della proiezione; chiave esterna obbligatoria.</summary>
    [Required]
    public int CinemaId { get; set; }

    /// <summary>Relazione con il cinema associato all'ordine.</summary>
    [ForeignKey(nameof(CinemaId))]
    public Cinema? Cinema { get; set; }

    /// <summary>Sala della proiezione; chiave esterna obbligatoria.</summary>
    [Required]
    public int SalaId { get; set; }

    /// <summary>Relazione con la sala associata all'ordine.</summary>
    [ForeignKey(nameof(SalaId))]
    public Sala? Sala { get; set; }

    /// <summary>Film acquistato; chiave esterna obbligatoria.</summary>
    [Required]
    public int FilmId { get; set; }

    /// <summary>Relazione con il film associato all'ordine.</summary>
    [ForeignKey(nameof(FilmId))]
    public Film? Film { get; set; }

    /// <summary>Token di hold dei posti che ha generato l'ordine; massimo 120 caratteri.</summary>
    [Required]
    [MaxLength(120)]
    public string HoldToken { get; set; } = string.Empty;

    /// <summary>Numero di biglietti inclusi nell'ordine.</summary>
    [Required]
    public int NumeroBiglietti { get; set; }

    /// <summary>Totale lordo dell'ordine prima di crediti o altri sconti.</summary>
    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal TotaleLordo { get; set; }

    /// <summary>Importo coperto da credito utente; valore monetario a due decimali.</summary>
    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal ImportoCredito { get; set; }

    /// <summary>Importo coperto da pagamento con carta; valore monetario a due decimali.</summary>
    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal ImportoCarta { get; set; }

    /// <summary>PaymentIntent Stripe associato, se presente; massimo 120 caratteri.</summary>
    [MaxLength(120)]
    public string? StripePaymentIntentId { get; set; }

    /// <summary>Checkout Session Stripe associata, se presente; massimo 120 caratteri.</summary>
    [MaxLength(120)]
    public string? StripeCheckoutSessionId { get; set; }

    /// <summary>Chiave di idempotenza per evitare doppie elaborazioni; massimo 120 caratteri.</summary>
    [MaxLength(120)]
    public string? IdempotencyKey { get; set; }

    /// <summary>Stato corrente dell'ordine nel ciclo di vita di acquisto.</summary>
    [Required]
    public OrdineState Stato { get; set; }

    /// <summary>Data/ora UTC di creazione dell'ordine.</summary>
    [Required]
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>Data/ora UTC di pagamento dell'ordine; nulla se non pagato.</summary>
    public DateTime? PaidAtUtc { get; set; }

    /// <summary>Data/ora UTC di scadenza del checkout; nulla se non applicabile.</summary>
    public DateTime? CheckoutExpiresAtUtc { get; set; }

    /// <summary>Data/ora UTC di completamento del checkout; nulla se non concluso.</summary>
    public DateTime? CheckoutCompletedAtUtc { get; set; }

    /// <summary>Data/ora UTC di invio dell'email biglietti; nulla se non inviata.</summary>
    public DateTime? TicketEmailSentAtUtc { get; set; }

    /// <summary>Ultimo errore di invio email biglietti; massimo 1000 caratteri.</summary>
    [MaxLength(1000)]
    public string? TicketEmailLastError { get; set; }

    /// <summary>Ultimo errore di pagamento registrato; massimo 1000 caratteri.</summary>
    [MaxLength(1000)]
    public string? LastPaymentError { get; set; }

    /// <summary>Credito riservato per pagamenti misti; importo a due decimali.</summary>
    [Column(TypeName = "decimal(10,2)")]
    public decimal CreditoRiservato { get; set; }

    /// <summary>Biglietti associati all'ordine.</summary>
    public ICollection<Biglietto> Biglietti { get; set; } = new List<Biglietto>();
}
