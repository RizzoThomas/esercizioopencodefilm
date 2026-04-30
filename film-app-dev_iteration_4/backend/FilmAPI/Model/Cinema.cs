using System.ComponentModel.DataAnnotations;

namespace FilmAPI.Model;

public class Cinema
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Nome { get; set; } = string.Empty;

    [Required]
    [MaxLength(300)]
    public string Indirizzo { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Citta { get; set; } = string.Empty;

    public double? Latitudine { get; set; }

    public double? Longitudine { get; set; }

    [MaxLength(20)]
    public string? Telefono { get; set; }

    [MaxLength(50)]
    public string? CodiceLocale { get; set; }

    public ICollection<Proiezione> Proiezioni { get; set; } = new List<Proiezione>();
    public ICollection<Sala> Sale { get; set; } = new List<Sala>();
}