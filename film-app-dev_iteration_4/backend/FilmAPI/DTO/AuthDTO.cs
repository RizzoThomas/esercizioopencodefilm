using System.ComponentModel.DataAnnotations;

namespace FilmAPI.DTO;

/// <summary>DTO di richiesta usato da POST /api/auth/login.</summary>
/// <summary>Rappresenta il login con email, password e device opzionale.</summary>
public class LoginRequestDTO
{
    /// <summary>Email dell'utente; serve a identificare l'account e viene validata come indirizzo valido.</summary>
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    /// <summary>Password dell'utente; è richiesta per l'autenticazione locale.</summary>
    [Required]
    public string Password { get; set; } = string.Empty;

    /// <summary>Identificativo del dispositivo; limita e traccia la sessione sul device.</summary>
    [MaxLength(128)]
    public string? DeviceId { get; set; }
}

/// <summary>DTO di richiesta usato da POST /api/auth/register.</summary>
/// <summary>Rappresenta la creazione di un nuovo account utente.</summary>
public class RegisterRequestDTO
{
    /// <summary>Email del nuovo utente; deve essere valida e univoca.</summary>
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    /// <summary>Password scelta dall'utente; serve per le credenziali locali.</summary>
    [Required]
    public string Password { get; set; } = string.Empty;

    /// <summary>Nome dell'utente; obbligatorio per il profilo e la comunicazione.</summary>
    [Required]
    [MaxLength(100)]
    public string Nome { get; set; } = string.Empty;

    /// <summary>Cognome dell'utente; obbligatorio per il profilo e la comunicazione.</summary>
    [Required]
    [MaxLength(100)]
    public string Cognome { get; set; } = string.Empty;

    /// <summary>Telefono opzionale; utile per notifiche e recupero account.</summary>
    [MaxLength(20)]
    public string? Telefono { get; set; }

    /// <summary>Identificativo del dispositivo; serve a legare il primo refresh token al device.</summary>
    [MaxLength(128)]
    public string? DeviceId { get; set; }
}

/// <summary>DTO di risposta usato da login e refresh token.</summary>
/// <summary>Contiene token e dati utente autenticato.</summary>
public class AuthResponseDTO
{
    /// <summary>Access token JWT breve; va inviato nelle richieste protette.</summary>
    public string AccessToken { get; set; } = string.Empty;
    /// <summary>Refresh token lungo; serve per rinnovare l'access token.</summary>
    public string RefreshToken { get; set; } = string.Empty;
    /// <summary>Data UTC di scadenza del refresh token.</summary>
    public DateTime ExpiresAt { get; set; }
    /// <summary>Dati sintetici dell'utente autenticato; servono al frontend.</summary>
    public UserInfoDTO User { get; set; } = new();
    /// <summary>Indica se serve il secondo fattore per completare il login.</summary>
    public bool RequiresTwoFactor { get; set; }
    /// <summary>Token temporaneo usato nel flusso 2FA.</summary>
    public string? TempToken { get; set; }
    /// <summary>Token del dispositivo fidato, se emesso.</summary>
    public string? TrustedDeviceToken { get; set; }
}

/// <summary>DTO di informazioni pubbliche dell'utente autenticato.</summary>
public class UserInfoDTO
{
    /// <summary>ID univoco dell'utente.</summary>
    public int Id { get; set; }
    /// <summary>Email dell'utente.</summary>
    public string Email { get; set; } = string.Empty;
    /// <summary>Nome dell'utente.</summary>
    public string Nome { get; set; } = string.Empty;
    /// <summary>Cognome dell'utente.</summary>
    public string Cognome { get; set; } = string.Empty;
    /// <summary>Telefono opzionale.</summary>
    public string? Telefono { get; set; }
    /// <summary>Ruolo dell'utente nel sistema.</summary>
    public string Ruolo { get; set; } = string.Empty;
    /// <summary>Data di registrazione.</summary>
    public DateTime DataRegistrazione { get; set; }
    /// <summary>Indica se il 2FA è attivo.</summary>
    public bool TwoFactorEnabled { get; set; }
}

/// <summary>DTO di sottoscrizione utente a un abbonamento.</summary>
public class UserSubscriptionDTO
{
    /// <summary>ID della sottoscrizione.</summary>
    public int Id { get; set; }
    /// <summary>ID abbonamento di catalogo.</summary>
    public int AbbonamentoId { get; set; }
    /// <summary>Nome dell'abbonamento.</summary>
    public string AbbonamentoNome { get; set; } = string.Empty;
    /// <summary>Tipo dell'abbonamento.</summary>
    public string AbbonamentoTipo { get; set; } = string.Empty;
    /// <summary>Metodo di pagamento associato.</summary>
    public string MetodoPagamento { get; set; } = string.Empty;
    /// <summary>Indica se il rinnovo automatico è attivo.</summary>
    public bool AutoRinnovo { get; set; }
    /// <summary>Data di inizio della sottoscrizione.</summary>
    public DateTime DataInizio { get; set; }
    /// <summary>Data di scadenza della sottoscrizione.</summary>
    public DateTime DataScadenza { get; set; }
    /// <summary>Stato della sottoscrizione.</summary>
    public string Stato { get; set; } = string.Empty;
    /// <summary>Numero di biglietti mensili inclusi.</summary>
    public int NumeroBigliettiPerMese { get; set; }
    /// <summary>Numero di popcorn mensili inclusi.</summary>
    public int IncludePopcornPerMese { get; set; }
}

public class UserVoucherDTO
{
    /// <summary>ID del voucher.</summary>
    public int Id { get; set; }
    /// <summary>Codice del voucher.</summary>
    public string Codice { get; set; } = string.Empty;
    /// <summary>Valore del voucher.</summary>
    public decimal Importo { get; set; }
    /// <summary>Scadenza del voucher, se presente.</summary>
    public DateTime? DataScadenza { get; set; }
    /// <summary>Stato del voucher.</summary>
    public string Stato { get; set; } = string.Empty;
}

public class RefreshTokenRequestDTO
{
    /// <summary>Refresh token da validare.</summary>
    [Required]
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>ID dispositivo opzionale; serve a limitare il token al device.</summary>
    [MaxLength(128)]
    public string? DeviceId { get; set; }
}

// ─── Password Reset ────────────────────────────────────────────────

public class ForgotPasswordRequestDTO
{
    /// <summary>Email dell'account da recuperare.</summary>
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}

public class ResetPasswordRequestDTO
{
    /// <summary>Token di reset password.</summary>
    [Required]
    public string Token { get; set; } = string.Empty;

    /// <summary>Nuova password; minimo 8 caratteri per sicurezza.</summary>
    [Required]
    [MinLength(8)]
    public string NewPassword { get; set; } = string.Empty;
}

// ─── Change Password ───────────────────────────────────────────────

public class ChangePasswordRequestDTO
{
    /// <summary>Password corrente; serve a verificare l'identità prima del cambio.</summary>
    [Required]
    public string CurrentPassword { get; set; } = string.Empty;

    /// <summary>Nuova password; minimo 8 caratteri per rafforzare la sicurezza.</summary>
    [Required]
    [MinLength(8)]
    public string NewPassword { get; set; } = string.Empty;
}

// ─── Set Password (social-only → locale) ───────────────────────────

public class SetPasswordRequestDTO
{
    /// <summary>Nuova password per account social-only che viene convertito a locale.</summary>
    [Required]
    [MinLength(8)]
    public string NewPassword { get; set; } = string.Empty;
}

// ─── External Auth ──────────────────────────────────────────────────

public class ExternalExchangeRequestDTO
{
    /// <summary>Codice OAuth ricevuto dal provider esterno.</summary>
    [Required]
    public string Code { get; set; } = string.Empty;
}

public class ExternalProviderDTO
{
    /// <summary>Nome tecnico del provider.</summary>
    public string Provider { get; set; } = string.Empty;
    /// <summary>Nome visualizzato del provider.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>URL di avvio del login esterno.</summary>
    public string StartUrl { get; set; } = string.Empty;
}

// ─── Account Security ──────────────────────────────────────────────

public class AccountSecurityDTO
{
    /// <summary>Indica se è presente una password locale.</summary>
    public bool HasLocalPassword { get; set; }
    /// <summary>Data di cambio password, se disponibile.</summary>
    public DateTime? PasswordChangedAtUtc { get; set; }
    /// <summary>Provider esterni collegati all'account.</summary>
    public List<ExternalProviderDTO> LinkedProviders { get; set; } = new();
    /// <summary>Versione di autenticazione per invalidazione token.</summary>
    public int AuthVersion { get; set; }
}

// ─── 2FA ────────────────────────────────────────────────────────────

public class TwoFactorSetupResponseDTO
{
    /// <summary>Secret TOTP condiviso con l'app autenticatore.</summary>
    public string Secret { get; set; } = string.Empty;
    /// <summary>QR code in base64 per configurare il 2FA.</summary>
    public string QrCodeBase64 { get; set; } = string.Empty;
    /// <summary>Chiave manuale per setup alternativo.</summary>
    public string ManualKey { get; set; } = string.Empty;
}

public class TwoFactorEnableRequestDTO
{
    /// <summary>Codice 2FA a 6 cifre; è obbligatorio per confermare l'attivazione.</summary>
    [Required]
    [StringLength(6, MinimumLength = 6)]
    public string Code { get; set; } = string.Empty;
}

public class TwoFactorLoginRequestDTO
{
    /// <summary>Token temporaneo del primo step di login.</summary>
    [Required]
    public string TempToken { get; set; } = string.Empty;

    /// <summary>Codice 2FA a 6 cifre.</summary>
    [Required]
    [StringLength(6, MinimumLength = 6)]
    public string Code { get; set; } = string.Empty;

    /// <summary>Indica se il device va considerato fidato.</summary>
    public bool TrustDevice { get; set; }

    /// <summary>ID dispositivo opzionale per associare il trust al device.</summary>
    [MaxLength(128)]
    public string? DeviceId { get; set; }
}

/// <summary>DTO di risposta che indica la richiesta del secondo fattore.</summary>
public class TwoFactorRequiredResponseDTO
{
    /// <summary>Indica che il secondo fattore è richiesto.</summary>
    public bool RequiresTwoFactor { get; set; }
    /// <summary>Token temporaneo da usare nel flusso 2FA.</summary>
    public string TempToken { get; set; } = string.Empty;
}

/// <summary>DTO di stato 2FA per il profilo utente.</summary>
public class TwoFactorStatusDTO
{
    /// <summary>Indica se il 2FA è abilitato.</summary>
    public bool Enabled { get; set; }
}

/// <summary>DTO legacy per la verifica 2FA; mantenuto per compatibilità.</summary>
[System.Obsolete("Usare TwoFactorLoginRequestDTO")]
public class TwoFactorVerifyRequestDTO
{
    /// <summary>Email dell'utente; serve a identificare l'account legacy.</summary>
    [Required][EmailAddress] public string Email { get; set; } = string.Empty;
    /// <summary>Password dell'utente legacy.</summary>
    [Required] public string Password { get; set; } = string.Empty;
    /// <summary>Codice 2FA a 6 cifre.</summary>
    [Required][StringLength(6, MinimumLength = 6)] public string Code { get; set; } = string.Empty;
    /// <summary>ID dispositivo opzionale.</summary>
    [MaxLength(128)] public string? DeviceId { get; set; }
}
