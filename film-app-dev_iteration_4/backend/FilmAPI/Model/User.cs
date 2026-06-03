using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FilmAPI.Model;

/// <summary>
/// Utente registrato della piattaforma CineBase.
/// È usato dai servizi di autenticazione, checkout, credito e assistenza e mappa la tabella utenti nel database.
/// </summary>
public class User
{
    /// <summary>Identificativo univoco dell'utente.</summary>
    [Key]
    public int Id { get; set; }

    /// <summary>Email principale dell'utente; obbligatoria e massima 255 caratteri.</summary>
    [Required]
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    /// <summary>Email normalizzata usata per lookup case-insensitive; massima 255 caratteri.</summary>
    [Required]
    [MaxLength(255)]
    public string NormalizedEmail { get; set; } = string.Empty;

    /// <summary>Hash della password locale; nulla se l'account usa solo login esterni.</summary>
    public string? PasswordHash { get; set; }

    /// <summary>Indica se le credenziali locali sono abilitate per l'utente.</summary>
    [Required]
    public bool LocalCredentialsEnabled { get; set; } = true;

    /// <summary>Data/ora UTC di verifica email; nulla se non confermata.</summary>
    public DateTime? EmailVerifiedAtUtc { get; set; }

    /// <summary>Data/ora UTC dell'ultimo cambio password.</summary>
    public DateTime? PasswordChangedAtUtc { get; set; }

    /// <summary>Obbliga l'utente a cambiare password al prossimo login.</summary>
    [Required]
    public bool MustChangePassword { get; set; } = false;

    /// <summary>Versione di sicurezza usata per invalidare vecchi token JWT.</summary>
    [Required]
    public int AuthVersion { get; set; } = 0;

    /// <summary>Indica se l'autenticazione a due fattori è abilitata.</summary>
    [Required]
    public bool TwoFactorEnabled { get; set; } = false;

    /// <summary>Secret TOTP per l'autenticazione a due fattori; massimo 128 caratteri.</summary>
    [MaxLength(128)]
    public string? TwoFactorSecret { get; set; }

    /// <summary>Data/ora UTC dell'ultimo login; nulla se l'utente non ha ancora effettuato accessi.</summary>
    public DateTime? LastLoginAtUtc { get; set; }

    /// <summary>Provider usato per l'ultimo login; massimo 30 caratteri.</summary>
    [MaxLength(30)]
    public string? LastLoginProvider { get; set; }

    /// <summary>Indica se l'account è disabilitato.</summary>
    [Required]
    public bool IsDisabled { get; set; } = false;

    /// <summary>Data/ora UTC di anonimizzazione dell'account; nulla se non anonimizzato.</summary>
    public DateTime? AnonymizedAtUtc { get; set; }

    /// <summary>Versione della privacy policy accettata; massimo 50 caratteri.</summary>
    [MaxLength(50)]
    public string? PrivacyPolicyVersion { get; set; }

    /// <summary>Data/ora UTC di accettazione della privacy policy; nulla se non accettata.</summary>
    public DateTime? PrivacyPolicyAcceptedAtUtc { get; set; }

    /// <summary>Versione dei termini di servizio accettata; massimo 50 caratteri.</summary>
    [MaxLength(50)]
    public string? TermsAcceptedVersion { get; set; }

    /// <summary>Data/ora UTC di accettazione dei termini di servizio; nulla se non accettati.</summary>
    public DateTime? TermsAcceptedAtUtc { get; set; }

    /// <summary>Nome dell'utente; massimo 100 caratteri.</summary>
    [Required]
    [MaxLength(100)]
    public string Nome { get; set; } = string.Empty;

    /// <summary>Cognome dell'utente; massimo 100 caratteri.</summary>
    [Required]
    [MaxLength(100)]
    public string Cognome { get; set; } = string.Empty;

    /// <summary>Numero di telefono facoltativo; massimo 20 caratteri.</summary>
    [MaxLength(20)]
    public string? Telefono { get; set; }

    /// <summary>Ruolo autorizzativo dell'utente nella piattaforma.</summary>
    [Required]
    public UserRole Ruolo { get; set; }

    /// <summary>Data/ora UTC di registrazione dell'account.</summary>
    [Required]
    public DateTime DataRegistrazione { get; set; }

    /// <summary>Cinema preferito dell'utente; chiave esterna opzionale.</summary>
    public int? CinemaPreferitoId { get; set; }

    /// <summary>Relazione con il cinema preferito dall'utente.</summary>
    [ForeignKey(nameof(CinemaPreferitoId))]
    public Cinema? CinemaPreferito { get; set; }

    /// <summary>Credito residuo disponibile; importo monetario a due decimali.</summary>
    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal CreditoResiduo { get; set; }

    /// <summary>Token di refresh associati all'utente.</summary>
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    /// <summary>Prenotazioni legacy associate all'utente.</summary>
    public ICollection<Prenotazione> Prenotazioni { get; set; } = new List<Prenotazione>();
    /// <summary>Ordini effettuati dall'utente.</summary>
    public ICollection<Ordine> Ordini { get; set; } = new List<Ordine>();
    /// <summary>Biglietti intestati all'utente.</summary>
    public ICollection<Biglietto> Biglietti { get; set; } = new List<Biglietto>();
    /// <summary>Sottoscrizioni utente attive.</summary>
    public ICollection<UserSubscription> UserSubscriptions { get; set; } = new List<UserSubscription>();
    /// <summary>Login esterni collegati all'account.</summary>
    public ICollection<UserExternalLogin> ExternalLogins { get; set; } = new List<UserExternalLogin>();
    /// <summary>Token di azione account associati all'utente.</summary>
    public ICollection<AccountActionToken> ActionTokens { get; set; } = new List<AccountActionToken>();

    /// <summary>Token legacy di reset password; massimo 128 caratteri.</summary>
    [MaxLength(128)]
    public string? PasswordResetToken { get; set; }

    /// <summary>Data/ora di scadenza del token legacy di reset password.</summary>
    public DateTime? ResetTokenExpiry { get; set; }
}
