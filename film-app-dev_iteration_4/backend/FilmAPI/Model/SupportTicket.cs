using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FilmAPI.Model;

/// <summary>
/// Ticket di supporto aperto da un utente della piattaforma CineBase.
/// È usato dal servizio assistenza per tracciare richieste, priorità e stato di lavorazione nel database.
/// </summary>
public class SupportTicket
{
    /// <summary>Identificativo univoco del ticket di supporto.</summary>
    [Key]
    public int Id { get; set; }

    /// <summary>Utente che ha aperto il ticket; chiave esterna opzionale per richieste anonime o ospiti.</summary>
    public int? UserId { get; set; }

    /// <summary>Relazione con l'utente mittente del ticket, se presente.</summary>
    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }

    /// <summary>Oggetto sintetico della richiesta; massimo 200 caratteri.</summary>
    [Required]
    [MaxLength(200)]
    public string Oggetto { get; set; } = string.Empty;

    /// <summary>Messaggio completo della richiesta; massimo 4000 caratteri.</summary>
    [Required]
    [MaxLength(4000)]
    public string Messaggio { get; set; } = string.Empty;

    /// <summary>Email di contatto alternativa; massimo 200 caratteri.</summary>
    [MaxLength(200)]
    public string? EmailContatto { get; set; }

    /// <summary>Stato operativo del ticket nel workflow di assistenza.</summary>
    public TicketStato Stato { get; set; } = TicketStato.Aperto;

    /// <summary>Data/ora UTC di creazione del ticket.</summary>
    public DateTime CreatoIl { get; set; } = DateTime.UtcNow;

    /// <summary>Data/ora UTC di risoluzione del ticket; nulla finché il ticket è aperto.</summary>
    public DateTime? RisoltoIl { get; set; }
}

/// <summary>
/// Stato del ticket di supporto nella piattaforma CineBase.
/// </summary>
public enum TicketStato
{
    /// <summary>Ticket aperto e non ancora preso in carico.</summary>
    Aperto = 0,
    /// <summary>Ticket assegnato a un operatore.</summary>
    InCarico = 1,
    /// <summary>Ticket risolto ma ancora visibile nello storico.</summary>
    Risolto = 2,
    /// <summary>Ticket chiuso definitivamente.</summary>
    Chiuso = 3
}
