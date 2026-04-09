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
    public User User { get; set; } = null!;

    [Required]
    public int ProiezioneId { get; set; }

    [ForeignKey(nameof(ProiezioneId))]
    public Proiezione Proiezione { get; set; } = null!;

    [Required]
    public int NumeroPosti { get; set; } = 1;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public StatoPrenotazione Stato { get; set; } = StatoPrenotazione.InAttesa;

    [MaxLength(50)]
    public string? CodicePrenotazione { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal? PrezzoTotale { get; set; }

    [MaxLength(2000)]
    public string? PostiSelezionati { get; set; }
}

public enum StatoPrenotazione
{
    InAttesa = 0,
    Confermata = 1,
    Annullata = 2
}
