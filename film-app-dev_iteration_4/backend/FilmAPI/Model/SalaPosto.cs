using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FilmAPI.Model;

public class SalaPosto
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int SalaId { get; set; }

    [ForeignKey(nameof(SalaId))]
    public Sala? Sala { get; set; }

    [Required]
    [MaxLength(50)]
    public string Settore { get; set; } = "PLATEA";

    [Required]
    public int Fila { get; set; }

    [Required]
    public int Numero { get; set; }

    public int? PosX { get; set; }

    public int? PosY { get; set; }

    [Required]
    public bool IsWheelchair { get; set; }

    [Required]
    public bool IsAttivo { get; set; } = true;
}