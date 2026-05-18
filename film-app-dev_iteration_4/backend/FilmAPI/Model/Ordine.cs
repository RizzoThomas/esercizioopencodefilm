// ============================================================================
// Ordine.cs — ENTITÀ ORDINE (CENTRALE NEL FLUSSO DI ACQUISTO)
// ============================================================================
// Rappresenta un ordine di acquisto biglietti.
// Passa attraverso diversi stati (OrdineState):
//   Pending → CheckoutInProgress → Paid
//   Pending → Cancelled / Expired
//   CheckoutInProgress → Cancelled / Expired
//
// L'ordine contiene TUTTA l'informazione necessaria:
// - Cosa è stato acquistato (film, cinema, sala, show)
// - Quanto è costato (totale, credito, carta)
// - Come è stato pagato (StripeSessionId)
// - Tracciamento email (TicketEmailSentAt)
// ============================================================================

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FilmAPI.Model;

public class Ordine
{
    [Key]
    public int Id { get; set; }

    // ─── CODICE ORDINE (leggibile dall'utente) ────────────────────────────
    // Esempio: "CB-A1B2C3D4"
    // Indice univoco su questa colonna
    [Required]
    [MaxLength(50)]
    public string CodiceOrdine { get; set; } = string.Empty;

    // ─── FOREIGN KEYS ─────────────────────────────────────────────────────
    // L'ordine è collegato a TUTTE le entità coinvolte nell'acquisto:
    // User: chi ha acquistato
    // Show: quale spettacolo
    // Cinema/Sala/Film: snapshot dell'acquisto (anche se i dati cambiano dopo)
    [Required]
    public int UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }

    [Required]
    public int ShowId { get; set; }

    [ForeignKey(nameof(ShowId))]
    public Show? Show { get; set; }

    [Required]
    public int CinemaId { get; set; }

    [ForeignKey(nameof(CinemaId))]
    public Cinema? Cinema { get; set; }

    [Required]
    public int SalaId { get; set; }

    [ForeignKey(nameof(SalaId))]
    public Sala? Sala { get; set; }

    [Required]
    public int FilmId { get; set; }

    [ForeignKey(nameof(FilmId))]
    public Film? Film { get; set; }

    // ─── HOLD TOKEN ───────────────────────────────────────────────────────
    // Token dell'hold posti da cui è stato creato questo ordine
    [Required]
    [MaxLength(120)]
    public string HoldToken { get; set; } = string.Empty;

    // ─── DATI ECONOMICI ───────────────────────────────────────────────────
    [Required]
    public int NumeroBiglietti { get; set; }               // Quanti biglietti

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal TotaleLordo { get; set; }               // Totale complessivo

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal ImportoCredito { get; set; }             // Quanto pagato con credito

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal ImportoCarta { get; set; }               // Quanto pagato con carta

    // ─── INTEGRAZIONE STRIPE ──────────────────────────────────────────────
    [MaxLength(120)]
    public string? StripePaymentIntentId { get; set; }     // Payment Intent (Stripe Elements legacy)

    [MaxLength(120)]
    public string? StripeCheckoutSessionId { get; set; }   // Checkout Session (Stripe Checkout hosted)

    // ─── IDEMPOTENZA ──────────────────────────────────────────────────────
    // Chiave generata dal client per evitare doppi pagamenti
    // Se stessa IdempotencyKey arriva due volte, il backend restituisce
    // lo stesso risultato senza processare un nuovo pagamento
    [MaxLength(120)]
    public string? IdempotencyKey { get; set; }

    // ─── STATO ORDINE ────────────────────────────────────────────────────
    // Enum OrdineState: Pending, Paid, Failed, Cancelled, Expired, CheckoutInProgress
    [Required]
    public OrdineState Stato { get; set; }

    // ─── TIMESTAMP ────────────────────────────────────────────────────────
    [Required]
    public DateTime CreatedAtUtc { get; set; }              // Data creazione

    public DateTime? PaidAtUtc { get; set; }                // Data pagamento

    // Campi Stripe Checkout
    public DateTime? CheckoutExpiresAtUtc { get; set; }     // Scadenza sessione Stripe
    public DateTime? CheckoutCompletedAtUtc { get; set; }   // Completamento sessione

    // Tracciamento email biglietti
    public DateTime? TicketEmailSentAtUtc { get; set; }     // Email inviata con successo
    [MaxLength(1000)]
    public string? TicketEmailLastError { get; set; }       // Errore invio email (se fallito)

    [MaxLength(1000)]
    public string? LastPaymentError { get; set; }           // Ultimo errore di pagamento

    // ─── CREDITO RISERVATO (pagamento misto) ──────────────────────────────
    // In un pagamento misto, il credito viene riservato al momento della
    // creazione della sessione Stripe e addebitato solo a pagamento confermato
    [Column(TypeName = "decimal(10,2)")]
    public decimal CreditoRiservato { get; set; }

    // ─── BIGLIETTI ASSOCIATI ──────────────────────────────────────────────
    // Un ordine contiene N biglietti (uno per posto acquistato)
    public ICollection<Biglietto> Biglietti { get; set; } = new List<Biglietto>();
}
