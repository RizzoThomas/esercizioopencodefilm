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
}

public class RefreshTokenRequestDTO
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;

    [MaxLength(128)]
    public string? DeviceId { get; set; }
}
