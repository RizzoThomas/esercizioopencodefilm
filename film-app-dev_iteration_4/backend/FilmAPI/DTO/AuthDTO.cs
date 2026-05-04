using System.ComponentModel.DataAnnotations;

namespace FilmAPI.DTO;

public class LoginRequestDTO
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;

    [MaxLength(128)]
    public string? DeviceId { get; set; }
}

public class RegisterRequestDTO
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Nome { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Cognome { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Telefono { get; set; }

    [MaxLength(128)]
    public string? DeviceId { get; set; }
}

public class AuthResponseDTO
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public UserInfoDTO User { get; set; } = new();
    public bool RequiresTwoFactor { get; set; }
    public string? TempToken { get; set; }
}

public class UserInfoDTO
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string Cognome { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string Ruolo { get; set; } = string.Empty;
    public DateTime DataRegistrazione { get; set; }
    public bool TwoFactorEnabled { get; set; }
}

public class RefreshTokenRequestDTO
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;

    [MaxLength(128)]
    public string? DeviceId { get; set; }
}

// ─── Password Reset ────────────────────────────────────────────────

public class ForgotPasswordRequestDTO
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}

public class ResetPasswordRequestDTO
{
    [Required]
    public string Token { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string NewPassword { get; set; } = string.Empty;
}

// ─── 2FA ────────────────────────────────────────────────────────────

public class TwoFactorSetupResponseDTO
{
    public string Secret { get; set; } = string.Empty;
    public string QrCodeBase64 { get; set; } = string.Empty;
    public string ManualKey { get; set; } = string.Empty;
}

public class TwoFactorEnableRequestDTO
{
    [Required]
    [StringLength(6, MinimumLength = 6)]
    public string Code { get; set; } = string.Empty;
}

public class TwoFactorLoginRequestDTO
{
    [Required]
    public string TempToken { get; set; } = string.Empty;

    [Required]
    [StringLength(6, MinimumLength = 6)]
    public string Code { get; set; } = string.Empty;

    public bool TrustDevice { get; set; }

    [MaxLength(128)]
    public string? DeviceId { get; set; }
}

public class TwoFactorRequiredResponseDTO
{
    public bool RequiresTwoFactor { get; set; }
    public string TempToken { get; set; } = string.Empty;
}

public class TwoFactorStatusDTO
{
    public bool Enabled { get; set; }
}

// Rimuovo TwoFactorVerifyRequestDTO (rimpiazzato da TwoFactorLoginRequestDTO)
[System.Obsolete("Usare TwoFactorLoginRequestDTO")]
public class TwoFactorVerifyRequestDTO
{
    [Required][EmailAddress] public string Email { get; set; } = string.Empty;
    [Required] public string Password { get; set; } = string.Empty;
    [Required][StringLength(6, MinimumLength = 6)] public string Code { get; set; } = string.Empty;
    [MaxLength(128)] public string? DeviceId { get; set; }
}
