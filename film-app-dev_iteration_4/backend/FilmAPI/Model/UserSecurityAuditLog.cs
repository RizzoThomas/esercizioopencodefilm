using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FilmAPI.Model;

/// <summary>
/// Log di audit per operazioni sensibili su account e credenziali.
/// Eventi: PasswordChanged, PasswordResetRequested, PasswordResetCompleted,
/// ExternalLoginSucceeded, ExternalLoginRejected*, RoleChanged, etc.
/// </summary>
public class UserSecurityAuditLog
{
    [Key]
    public int Id { get; set; }

    /// <summary>Utente target dell'operazione.</summary>
    public int? UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }

    /// <summary>Utente che ha eseguito l'operazione (null per azioni anonime o self-service).</summary>
    public int? ActorUserId { get; set; }

    [ForeignKey(nameof(ActorUserId))]
    public User? ActorUser { get; set; }

    /// <summary>Tipo evento (es. 'PasswordChanged', 'RoleChanged', 'ExternalLoginSucceeded').</summary>
    [Required]
    [MaxLength(80)]
    public string EventType { get; set; } = string.Empty;

    /// <summary>Provider esterno se applicabile ('Google', 'Microsoft', 'Facebook').</summary>
    [MaxLength(30)]
    public string? Provider { get; set; }

    [MaxLength(64)]
    public string? IpAddress { get; set; }

    [MaxLength(512)]
    public string? UserAgent { get; set; }

    /// <summary>JSON opzionale con metadati aggiuntivi (es. ruolo precedente/nuovo).</summary>
    [MaxLength(4000)]
    public string? MetadataJson { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
