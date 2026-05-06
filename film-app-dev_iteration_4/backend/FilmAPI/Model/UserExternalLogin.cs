using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FilmAPI.Model;

/// <summary>
/// Collegamento tra un utente CineBase e un provider di login esterno (Google, Microsoft, Facebook).
/// </summary>
public class UserExternalLogin
{
    [Key]
    public int Id { get; set; }

    public int UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    [Required]
    public ExternalLoginProvider Provider { get; set; }

    /// <summary>ID utente presso il provider (sub/oid per OIDC).</summary>
    [Required]
    [MaxLength(255)]
    public string ProviderUserId { get; set; } = string.Empty;

    /// <summary>Tenant ID Microsoft (se applicabile).</summary>
    [MaxLength(255)]
    public string? ProviderTenantId { get; set; }

    /// <summary>Email al momento del collegamento.</summary>
    [Required]
    [MaxLength(255)]
    public string EmailAtLogin { get; set; } = string.Empty;

    public DateTime LinkedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? LastLoginAtUtc { get; set; }

    /// <summary>Se valorizzato, il collegamento è stato revocato.</summary>
    public DateTime? RevokedAtUtc { get; set; }
}
