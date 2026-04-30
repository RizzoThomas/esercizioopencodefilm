using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FilmAPI.Model;

public class MovimentoCredito
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }

    [Required]
    public MovimentoCreditoTipo Tipo { get; set; }

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal Importo { get; set; }

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal SaldoPre { get; set; }

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal SaldoPost { get; set; }

    public int? OperatoreUserId { get; set; }

    [ForeignKey(nameof(OperatoreUserId))]
    public User? OperatoreUser { get; set; }

    public int? CinemaId { get; set; }

    [ForeignKey(nameof(CinemaId))]
    public Cinema? Cinema { get; set; }

    public int? OrdineId { get; set; }

    [ForeignKey(nameof(OrdineId))]
    public Ordine? Ordine { get; set; }

    [Required]
    public DateTime CreatedAtUtc { get; set; }

    [MaxLength(500)]
    public string? Note { get; set; }
}