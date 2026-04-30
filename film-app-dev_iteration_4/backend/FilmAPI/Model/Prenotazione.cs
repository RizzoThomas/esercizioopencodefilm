using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FilmAPI.Model;

public class Prenotazione
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }

    [Required]
    public int ProiezioneId { get; set; }

    [ForeignKey(nameof(ProiezioneId))]
    public Proiezione? Proiezione { get; set; }

    [Required]
    public int NumeroPosti { get; set; }

    [MaxLength(500)]
    public string? Note { get; set; }

    [Required]
    public DateTime DataPrenotazione { get; set; }
}
