using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FilmAPI.Model;

/// <summary>
/// Collegamento tra un utente CineBase e un provider di login esterno.
/// È usato dai servizi di login federato e mappa la tabella dei provider collegati all'utente.
/// </summary>
public class UserExternalLogin
{
    /// <summary>Identificativo univoco del collegamento esterno.</summary>
    [Key]
    public int Id { get; set; }

    /// <summary>Utente proprietario del collegamento; chiave esterna obbligatoria.</summary>
    public int UserId { get; set; }

    /// <summary>Relazione con l'utente proprietario del login esterno.</summary>
    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    /// <summary>Provider esterno usato per il collegamento.</summary>
    [Required]
    public ExternalLoginProvider Provider { get; set; }

    /// <summary>ID utente presso il provider; massimo 255 caratteri.</summary>
    [Required]
    [MaxLength(255)]
    public string ProviderUserId { get; set; } = string.Empty;

    /// <summary>Tenant ID Microsoft, se applicabile; massimo 255 caratteri.</summary>
    [MaxLength(255)]
    public string? ProviderTenantId { get; set; }

    /// <summary>Email al momento del collegamento; massimo 255 caratteri.</summary>
    [Required]
    [MaxLength(255)]
    public string EmailAtLogin { get; set; } = string.Empty;

    /// <summary>Data/ora UTC di collegamento dell'account esterno.</summary>
    public DateTime LinkedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>Data/ora UTC dell'ultimo login tramite questo provider; nulla se non ancora usato.</summary>
    public DateTime? LastLoginAtUtc { get; set; }

    /// <summary>Data/ora UTC di revoca del collegamento; nulla se ancora attivo.</summary>
    public DateTime? RevokedAtUtc { get; set; }
}
