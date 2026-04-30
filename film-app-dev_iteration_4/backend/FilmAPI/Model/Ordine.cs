using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FilmAPI.Model;

public class Ordine
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string CodiceOrdine { get; set; } = string.Empty;

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

    [Required]
    [MaxLength(120)]
    public string HoldToken { get; set; } = string.Empty;

    [Required]
    public int NumeroBiglietti { get; set; }

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal TotaleLordo { get; set; }

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal ImportoCredito { get; set; }

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal ImportoCarta { get; set; }

    [MaxLength(120)]
    public string? StripePaymentIntentId { get; set; }

    [MaxLength(120)]
    public string? StripeCheckoutSessionId { get; set; }

    [MaxLength(120)]
    public string? IdempotencyKey { get; set; }

    [Required]
    public OrdineState Stato { get; set; }

    [Required]
    public DateTime CreatedAtUtc { get; set; }

    public DateTime? PaidAtUtc { get; set; }

    public DateTime? CheckoutExpiresAtUtc { get; set; }

    public DateTime? CheckoutCompletedAtUtc { get; set; }

    public DateTime? TicketEmailSentAtUtc { get; set; }

    [MaxLength(1000)]
    public string? TicketEmailLastError { get; set; }

    [MaxLength(1000)]
    public string? LastPaymentError { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal CreditoRiservato { get; set; }

    public ICollection<Biglietto> Biglietti { get; set; } = new List<Biglietto>();
}
