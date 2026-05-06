using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FilmAPI.Model;

/// <summary>
/// Codice di scambio temporaneo (one-time) usato dal frontend per ottenere
/// JWT + refresh token dopo un social login completato lato backend.
/// Il codice viene passato via query string a social-login-complete.html,
/// che lo scambia via POST /auth/external/exchange con i token applicativi.
/// </summary>
public class ExternalAuthExchangeCode
{
    [Key]
    public int Id { get; set; }

    public int UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    /// <summary>Hash SHA-256 del codice di scambio.</summary>
    [Required]
    [MaxLength(128)]
    public string CodeHash { get; set; } = string.Empty;

    /// <summary>Path di redirect dopo il completamento.</summary>
    [Required]
    [MaxLength(512)]
    public string RedirectPath { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime ExpiresAtUtc { get; set; }

    public DateTime? ConsumedAtUtc { get; set; }

    public ExternalLoginProvider Provider { get; set; }
}
