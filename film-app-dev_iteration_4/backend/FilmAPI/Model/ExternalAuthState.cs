using System.ComponentModel.DataAnnotations;

namespace FilmAPI.Model;

/// <summary>
/// Stato temporaneo del flusso OAuth esterno (PKCE state, nonce, verifier).
/// Entità a vita breve — pulizia periodica necessaria.
/// </summary>
public class ExternalAuthState
{
    [Key]
    public int Id { get; set; }

    [Required]
    public ExternalLoginProvider Provider { get; set; }

    /// <summary>Hash SHA-256 dello state OAuth.</summary>
    [Required]
    [MaxLength(128)]
    public string StateHash { get; set; } = string.Empty;

    /// <summary>PKCE code verifier (necessario per scambiare il codice con il token).</summary>
    [Required]
    [MaxLength(256)]
    public string CodeVerifier { get; set; } = string.Empty;

    /// <summary>Nonce OIDC.</summary>
    [Required]
    [MaxLength(128)]
    public string Nonce { get; set; } = string.Empty;

    /// <summary>Path di redirect relativo dopo il login (es. '/profilo.html').</summary>
    [Required]
    [MaxLength(512)]
    public string RedirectPath { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime ExpiresAtUtc { get; set; }

    public DateTime? ConsumedAtUtc { get; set; }

    [MaxLength(64)]
    public string? RequestIp { get; set; }

    [MaxLength(512)]
    public string? UserAgent { get; set; }
}
