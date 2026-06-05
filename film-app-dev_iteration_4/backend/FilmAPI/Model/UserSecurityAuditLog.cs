using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FilmAPI.Model;

/// <summary>
/// Log di audit sicurezza dell'utente nella piattaforma CineBase.
/// È usato dai servizi di sicurezza e compliance per tracciare eventi sensibili nel database.
/// </summary>
public class UserSecurityAuditLog
{
    /// <summary>Identificativo univoco del log di audit.</summary>
    [Key]
    public int Id { get; set; }

    /// <summary>Utente a cui si riferisce l'evento, se noto.</summary>
    public int? UserId { get; set; }

    /// <summary>Utente attore che ha eseguito l'azione, se noto.</summary>
    public int? ActorUserId { get; set; }

    /// <summary>Tipo di evento di sicurezza; massimo 80 caratteri.</summary>
    [Required]
    [MaxLength(80)]
    public string EventType { get; set; } = string.Empty;

    /// <summary>Provider coinvolto nell'evento, se applicabile; massimo 30 caratteri.</summary>
    [MaxLength(30)]
    public string? Provider { get; set; }

    /// <summary>Indirizzo IP associato all'evento; massimo 64 caratteri.</summary>
    [MaxLength(64)]
    public string? IpAddress { get; set; }

    /// <summary>User-Agent del client che ha generato l'evento; massimo 512 caratteri.</summary>
    [MaxLength(512)]
    public string? UserAgent { get; set; }

    /// <summary>Dati aggiuntivi in formato JSON per il contesto di audit; massimo 4000 caratteri.</summary>
    [MaxLength(4000)]
    public string? MetadataJson { get; set; }

    /// <summary>Data/ora UTC di creazione del log.</summary>
    [Required]
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>Relazione con l'utente soggetto dell'audit.</summary>
    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }

    /// <summary>Relazione con l'utente attore che ha compiuto l'evento.</summary>
    [ForeignKey(nameof(ActorUserId))]
    public User? ActorUser { get; set; }
}
