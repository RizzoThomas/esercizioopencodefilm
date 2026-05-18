using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FilmAPI.Model;

/// <summary>
/// Token temporaneo per azioni sull'account nella piattaforma CineBase.
/// Serve a gestire reset password, impostazione password e inviti amministrativi e mappa la tabella dei token one-time.
/// </summary>
public class AccountActionToken
{
    /// <summary>Identificativo univoco del token account.</summary>
    [Key]
    public int Id { get; set; }

    /// <summary>Utente a cui il token è assegnato; chiave esterna obbligatoria.</summary>
    public int UserId { get; set; }

    /// <summary>Relazione con l'utente proprietario del token.</summary>
    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    /// <summary>Scopo business del token.</summary>
    [Required]
    public AccountActionTokenPurpose Purpose { get; set; }

    /// <summary>Hash SHA-256 del token; il valore in chiaro non viene mai persistito.</summary>
    [Required]
    [MaxLength(128)]
    public string TokenHash { get; set; } = string.Empty;

    /// <summary>Data/ora UTC di scadenza del token.</summary>
    [Required]
    public DateTime ExpiresAtUtc { get; set; }

    /// <summary>Data/ora UTC di creazione del token.</summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>Data/ora UTC di utilizzo del token; nulla finché non viene consumato.</summary>
    public DateTime? UsedAtUtc { get; set; }

    /// <summary>Data/ora UTC di revoca del token; nulla se ancora valido.</summary>
    public DateTime? RevokedAtUtc { get; set; }

    /// <summary>Utente admin che ha creato il token, quando presente.</summary>
    public int? CreatedByUserId { get; set; }

    /// <summary>Relazione con l'utente che ha creato il token.</summary>
    [ForeignKey(nameof(CreatedByUserId))]
    public User? CreatedByUser { get; set; }

    /// <summary>IP della richiesta che ha generato il token; massimo 64 caratteri.</summary>
    [MaxLength(64)]
    public string? RequestIp { get; set; }

    /// <summary>User-Agent della richiesta che ha generato il token; massimo 512 caratteri.</summary>
    [MaxLength(512)]
    public string? UserAgent { get; set; }
}
