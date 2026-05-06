using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FilmAPI.Model;

public class User
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    /// <summary>Email normalizzata (lowercase, trimmed) per lookup case-insensitive univoco.</summary>
    [Required]
    [MaxLength(255)]
    public string NormalizedEmail { get; set; } = string.Empty;

    /// <summary>Hash BCrypt della password. Null per account social-only (senza password locale).</summary>
    [MaxLength(255)]
    public string? PasswordHash { get; set; }

    /// <summary>True se l'utente ha credenziali locali attive (PasswordHash non nullo).</summary>
    public bool LocalCredentialsEnabled { get; set; } = true;

    [Required]
    [MaxLength(100)]
    public string Nome { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Cognome { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Telefono { get; set; }

    [Required]
    public UserRole Ruolo { get; set; }

    [Required]
    public DateTime DataRegistrazione { get; set; }

    public int? CinemaPreferitoId { get; set; }

    [ForeignKey(nameof(CinemaPreferitoId))]
    public Cinema? CinemaPreferito { get; set; }

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal CreditoResiduo { get; set; }

    // ─── Security / Auth ─────────────────────────────────────────────

    /// <summary>Versione di sicurezza: incrementata su cambio password, reset, cambio ruolo.
    /// Inclusa come claim 'auth_version' nel JWT; validata in OnTokenValidated.</summary>
    public int AuthVersion { get; set; }

    /// <summary>Timestamp ultimo login (qualsiasi provider).</summary>
    public DateTime? LastLoginAtUtc { get; set; }

    /// <summary>Provider usato per l'ultimo login ('Local', 'Google', 'Microsoft', 'Facebook').</summary>
    [MaxLength(30)]
    public string? LastLoginProvider { get; set; }

    /// <summary>Timestamp ultimo cambio password.</summary>
    public DateTime? PasswordChangedAtUtc { get; set; }

    /// <summary>Timestamp verifica email (via provider esterno o conferma manuale).</summary>
    public DateTime? EmailVerifiedAtUtc { get; set; }

    /// <summary>Se true, l'utente deve cambiare password al prossimo login.</summary>
    public bool MustChangePassword { get; set; }

    /// <summary>Se true, l'account è disabilitato (non può fare login).</summary>
    public bool IsDisabled { get; set; }

    // ─── Relazioni ────────────────────────────────────────────────────

    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<Prenotazione> Prenotazioni { get; set; } = new List<Prenotazione>();
    public ICollection<Ordine> Ordini { get; set; } = new List<Ordine>();
    public ICollection<Biglietto> Biglietti { get; set; } = new List<Biglietto>();
    public ICollection<UserExternalLogin> ExternalLogins { get; set; } = new List<UserExternalLogin>();
    public ICollection<AccountActionToken> ActionTokens { get; set; } = new List<AccountActionToken>();

    // Password Reset (legacy — mantenuto per compatibilità, verrà sostituito da AccountActionToken)
    [MaxLength(128)]
    public string? PasswordResetToken { get; set; }
    public DateTime? ResetTokenExpiry { get; set; }

    // 2FA
    [MaxLength(64)]
    public string? TwoFactorSecret { get; set; }
    public bool TwoFactorEnabled { get; set; }
}