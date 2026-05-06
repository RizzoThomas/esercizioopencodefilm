using System.ComponentModel.DataAnnotations;

namespace FilmAPI.DTO;

public class AdminUserListItemDTO
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string Cognome { get; set; } = string.Empty;
    public string Ruolo { get; set; } = string.Empty;
    public decimal CreditoResiduo { get; set; }
    public bool LocalCredentialsEnabled { get; set; }
    public bool IsDisabled { get; set; }
    public List<string> LinkedProviders { get; set; } = new();
    public DateTime DataRegistrazione { get; set; }
    public DateTime? LastLoginAtUtc { get; set; }
}

public class AdminUserPagedResultDTO
{
    public List<AdminUserListItemDTO> Items { get; set; } = new();
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class CreateAdminUserInviteDTO
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Nome { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Cognome { get; set; } = string.Empty;

    [Required]
    [RegularExpression("^(PowerUser|Admin)$")]
    public string Ruolo { get; set; } = string.Empty;
}

public class AdminUserSecurityDTO
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Ruolo { get; set; } = string.Empty;
    public bool HasLocalPassword { get; set; }
    public bool IsDisabled { get; set; }
    public DateTime? PasswordChangedAtUtc { get; set; }
    public int AuthVersion { get; set; }
    public List<LinkedProviderInfoDTO> LinkedProviders { get; set; } = new();
}

public class LinkedProviderInfoDTO
{
    public string Provider { get; set; } = string.Empty;
    public string EmailAtLogin { get; set; } = string.Empty;
    public DateTime LinkedAtUtc { get; set; }
    public DateTime? LastLoginAtUtc { get; set; }
}
