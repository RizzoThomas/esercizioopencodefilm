using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FilmAPI.Model;

public class Show
{
    [Key]
    public int Id { get; set; }

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
    public DateTime StartAtUtc { get; set; }

    [Required]
    public int DurataMinutiSnapshot { get; set; }

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal PrezzoBase { get; set; }

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal SupplementoSala { get; set; }

    public ICollection<ShowPostoStato> PostiStato { get; set; } = new List<ShowPostoStato>();
    public ICollection<Biglietto> Biglietti { get; set; } = new List<Biglietto>();
    public ICollection<Ordine> Ordini { get; set; } = new List<Ordine>();
}