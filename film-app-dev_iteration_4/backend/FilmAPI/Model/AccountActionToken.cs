using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FilmAPI.Model;

/// <summary>
/// Token temporaneo per azioni sull'account: reset password, setup password, invito admin.
/// Il token originale non viene mai salvato — solo il suo hash SHA-256.
/// </summary>
public class AccountActionToken
{
    [Key]
    public int Id { get; set; }

    public int UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    [Required]
    public AccountActionTokenPurpose Purpose { get; set; }

    /// <summary>Hash SHA-256 del token (il token in chiaro non viene mai persistito).</summary>
    [Required]
    [MaxLength(128)]
    public string TokenHash { get; set; } = string.Empty;

    [Required]
    public DateTime ExpiresAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UsedAtUtc { get; set; }

    public DateTime? RevokedAtUtc { get; set; }

    /// <summary>Utente admin che ha creato il token (per inviti).</summary>
    public int? CreatedByUserId { get; set; }

    [ForeignKey(nameof(CreatedByUserId))]
    public User? CreatedByUser { get; set; }

    [MaxLength(64)]
    public string? RequestIp { get; set; }

    [MaxLength(512)]
    public string? UserAgent { get; set; }
}
