using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FilmAPI.Model;

public class ShowPostoStato
{
    [Key]
    public int Id { get; set; }

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
    public ShowPostoState Stato { get; set; }

    [MaxLength(120)]
    public string? HoldToken { get; set; }

    public DateTime? ScadeAtUtc { get; set; }

    public int? OrdineId { get; set; }

    [ForeignKey(nameof(OrdineId))]
    public Ordine? Ordine { get; set; }

    [Required]
    public DateTime UpdatedAtUtc { get; set; }
}