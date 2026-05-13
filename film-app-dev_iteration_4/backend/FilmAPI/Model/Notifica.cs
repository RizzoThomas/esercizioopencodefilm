using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FilmAPI.Model;

public class Notifica
{
    [Key]
    public int Id { get; set; }

    public int UserId { get; set; }

    [Required]
    [MaxLength(50)]
    public string Tipo { get; set; } = string.Empty;  // biglietto, rimborso, offerta, promemoria, anteprima

    [Required]
    [MaxLength(200)]
    public string Titolo { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Descrizione { get; set; }

    [MaxLength(100)]
    public string? Icona { get; set; }  // fa-solid fa-ticket etc

    public bool Letto { get; set; } = false;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;
}
