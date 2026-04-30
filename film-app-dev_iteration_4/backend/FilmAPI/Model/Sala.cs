using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FilmAPI.Model;

public class Sala
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int CinemaId { get; set; }

    [ForeignKey(nameof(CinemaId))]
    public Cinema? Cinema { get; set; }

    [Required]
    public int NumeroProgressivo { get; set; }

    [Required]
    public TipoSala TipoSala { get; set; }

    [MaxLength(100)]
    public string? Nome { get; set; }

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal Supplemento { get; set; }

    [Required]
    public bool IsAttiva { get; set; } = true;

    public ICollection<SalaPosto> Posti { get; set; } = new List<SalaPosto>();
    public ICollection<Show> Shows { get; set; } = new List<Show>();
}