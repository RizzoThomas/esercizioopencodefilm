// ============================================================================
// User.cs — ENTITÀ UTENTE (AUTH, PROFILO, CREDITO)
// ============================================================================
// Rappresenta un utente registrato sulla piattaforma.
// Contiene sia dati anagrafici (nome, cognome) che dati di sicurezza
// (PasswordHash, AuthVersion, 2FA) e finanziari (CreditoResiduo).
// 
// ENUM UserRole:
//   User = 0       → Acquisto biglietti, profilo personale
//   PowerUser = 1  → Gestione film, registi, sale, show
//   Admin = 2      → Tutto + gestione utenti, ricariche credito
// ============================================================================

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FilmAPI.Model;

public class User
{
    [Key]
    public int Id { get; set; }

    // ─── CREDENZIALI ──────────────────────────────────────────────────────
    [Required]
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;             // Email dell'utente

    /// <summary>
    /// Email normalizzata (UPPERCASE, trimmed) per lookup case-insensitive.
    /// Viene auto-popolata in FilmDbContext.SaveChangesAsync()
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string NormalizedEmail { get; set; } = string.Empty;

    /// <summary>
    /// Hash BCrypt della password.
    /// Null per account creati tramite social login (senza password locale).
    /// </summary>
    [MaxLength(255)]
    public string? PasswordHash { get; set; }

    /// <summary>
    /// True se l'utente ha credenziali locali attive (PasswordHash non nullo).
    /// </summary>
    public bool LocalCredentialsEnabled { get; set; } = true;

    // ─── ANAGRAFICA ───────────────────────────────────────────────────────
    [Required]
    [MaxLength(100)]
    public string Nome { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Cognome { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Telefono { get; set; }

    [Required]
    public UserRole Ruolo { get; set; }                          // User/PowerUser/Admin

    [Required]
    public DateTime DataRegistrazione { get; set; }

    // ─── CINEMA PREFERITO ─────────────────────────────────────────────────
    // FK verso Cinema. L'utente può selezionare un cinema preferito
    // che viene sincronizzato tra localStorage e backend
    public int? CinemaPreferitoId { get; set; }

    [ForeignKey(nameof(CinemaPreferitoId))]
    public Cinema? CinemaPreferito { get; set; }

    // ─── CREDITO PIATTAFORMA ──────────────────────────────────────────────
    // Saldo del portafoglio digitale. Usato per pagamenti solo credito o misti.
    // [Column(TypeName = "decimal(10,2)")] → DECIMAL(10, 2) in MySQL
    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal CreditoResiduo { get; set; }

    // ─── SICUREZZA E AUTENTICAZIONE ───────────────────────────────────────

    /// <summary>
    /// Versione di sicurezza: incrementata su cambio password, reset, cambio ruolo.
    /// Inclusa come claim 'auth_version' nel JWT.
    /// Quando auth_version cambia, tutti i JWT emessi in precedenza diventano invalidi.
    /// Questo serve a forzare il logout dopo un cambio password.
    /// </summary>
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

    // ─── 2FA (Two-Factor Authentication) ───────────────────────────────────
    // Secret TOTP codificato in Base32 per Google Authenticator
    [MaxLength(64)]
    public string? TwoFactorSecret { get; set; }
    public bool TwoFactorEnabled { get; set; }

    // ─── COLLECTION NAVIGATION PROPERTIES ─────────────────────────────────
    // Relazioni 1:N con altre entità:
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<Prenotazione> Prenotazioni { get; set; } = new List<Prenotazione>();  // Legacy
    public ICollection<Ordine> Ordini { get; set; } = new List<Ordine>();
    public ICollection<Biglietto> Biglietti { get; set; } = new List<Biglietto>();
    public ICollection<UserSubscription> UserSubscriptions { get; set; } = new List<UserSubscription>();
    public ICollection<UserExternalLogin> ExternalLogins { get; set; } = new List<UserExternalLogin>();
    public ICollection<AccountActionToken> ActionTokens { get; set; } = new List<AccountActionToken>();

    // Legacy: password reset (sostituito da AccountActionToken)
    [MaxLength(128)]
    public string? PasswordResetToken { get; set; }
    public DateTime? ResetTokenExpiry { get; set; }
}
