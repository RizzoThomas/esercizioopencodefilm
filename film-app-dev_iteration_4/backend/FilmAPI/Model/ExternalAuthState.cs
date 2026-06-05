using System.ComponentModel.DataAnnotations;

namespace FilmAPI.Model;

/// <summary>
/// Stato temporaneo del flusso OAuth esterno della piattaforma CineBase.
/// Entità a vita breve usata per memorizzare state, nonce e verifier del login federato.
/// </summary>
public class ExternalAuthState
{
    /// <summary>Identificativo univoco dello stato OAuth.</summary>
    [Key]
    public int Id { get; set; }

    /// <summary>Provider esterno coinvolto nel flusso di autenticazione.</summary>
    [Required]
    public ExternalLoginProvider Provider { get; set; }

    /// <summary>Hash SHA-256 dello state OAuth; massimo 128 caratteri.</summary>
    [Required]
    [MaxLength(128)]
    public string StateHash { get; set; } = string.Empty;

    /// <summary>PKCE code verifier; massimo 256 caratteri.</summary>
    [Required]
    [MaxLength(256)]
    public string CodeVerifier { get; set; } = string.Empty;

    /// <summary>Nonce OIDC; massimo 128 caratteri.</summary>
    [Required]
    [MaxLength(128)]
    public string Nonce { get; set; } = string.Empty;

    /// <summary>Percorso di redirect relativo dopo il login; massimo 512 caratteri.</summary>
    [Required]
    [MaxLength(512)]
    public string RedirectPath { get; set; } = string.Empty;

    /// <summary>Data/ora UTC di creazione dello stato.</summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>Data/ora UTC di scadenza dello stato.</summary>
    public DateTime ExpiresAtUtc { get; set; }

    /// <summary>Data/ora UTC di consumo dello stato; nulla finché il callback non è completato.</summary>
    public DateTime? ConsumedAtUtc { get; set; }

    /// <summary>IP della richiesta originaria; massimo 64 caratteri.</summary>
    [MaxLength(64)]
    public string? RequestIp { get; set; }

    /// <summary>User-Agent della richiesta originaria; massimo 512 caratteri.</summary>
    [MaxLength(512)]
    public string? UserAgent { get; set; }
}
