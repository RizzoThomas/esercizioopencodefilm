using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FilmAPI.Model;

public class SupportTicket
{
    [Key]
    public int Id { get; set; }

    public int? UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }

    [Required]
    [MaxLength(200)]
    public string Oggetto { get; set; } = string.Empty;

    [Required]
    [MaxLength(4000)]
    public string Messaggio { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? EmailContatto { get; set; }

    public TicketStato Stato { get; set; } = TicketStato.Aperto;

    public DateTime CreatoIl { get; set; } = DateTime.UtcNow;

    public DateTime? RisoltoIl { get; set; }
}

public enum TicketStato
{
    Aperto = 0,
    InCarico = 1,
    Risolto = 2,
    Chiuso = 3
}
