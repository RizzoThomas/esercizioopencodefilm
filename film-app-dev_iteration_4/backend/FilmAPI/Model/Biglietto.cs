using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FilmAPI.Model;

public class Biglietto
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int OrdineId { get; set; }

    [ForeignKey(nameof(OrdineId))]
    public Ordine? Ordine { get; set; }

    [Required]
    public int ShowId { get; set; }

    [ForeignKey(nameof(ShowId))]
    public Show? Show { get; set; }

    [Required]
    public int SalaPostoId { get; set; }

    [ForeignKey(nameof(SalaPostoId))]
    public SalaPosto? SalaPosto { get; set; }

    [Required]
    public int UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }

    [Required]
    [MaxLength(50)]
    public string CodiceBiglietto { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string BarcodeValue { get; set; } = string.Empty;

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal PrezzoBase { get; set; }

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal Supplemento { get; set; }

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal PrezzoTotale { get; set; }

    [Required]
    public BigliettoState Stato { get; set; }

    public DateTime? ValidatoAtUtc { get; set; }

    public int? ValidatoDaUserId { get; set; }

    [ForeignKey(nameof(ValidatoDaUserId))]
    public User? ValidatoDaUser { get; set; }

    public int? ValidatoCinemaId { get; set; }

    [ForeignKey(nameof(ValidatoCinemaId))]
    public Cinema? ValidatoCinema { get; set; }
}
