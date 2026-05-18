using System.ComponentModel.DataAnnotations;

namespace FilmAPI.DTO;

/// <summary>DTO utente usato dalle API di amministrazione.</summary>
public class AdminUserListItemDTO
{
    /// <summary>ID univoco dell'utente.</summary>
    public int Id { get; set; }
    /// <summary>Email dell'utente.</summary>
    public string Email { get; set; } = string.Empty;
    /// <summary>Nome dell'utente.</summary>
    public string Nome { get; set; } = string.Empty;
    /// <summary>Cognome dell'utente.</summary>
    public string Cognome { get; set; } = string.Empty;
    /// <summary>Ruolo dell'utente.</summary>
    public string Ruolo { get; set; } = string.Empty;
    /// <summary>Credito residuo disponibile.</summary>
    public decimal CreditoResiduo { get; set; }
    /// <summary>Indica se le credenziali locali sono abilitate.</summary>
    public bool LocalCredentialsEnabled { get; set; }
    /// <summary>Indica se l'account è disabilitato.</summary>
    public bool IsDisabled { get; set; }
    /// <summary>Provider collegati all'account.</summary>
    public List<string> LinkedProviders { get; set; } = new();
    /// <summary>Data di registrazione.</summary>
    public DateTime DataRegistrazione { get; set; }
    /// <summary>Data dell'ultimo login, se presente.</summary>
    public DateTime? LastLoginAtUtc { get; set; }
}

/// <summary>Risultato paginato per la lista utenti admin.</summary>
public class AdminUserPagedResultDTO
{
    /// <summary>Elementi della pagina.</summary>
    public List<AdminUserListItemDTO> Items { get; set; } = new();
    /// <summary>Totale record.</summary>
    public int Total { get; set; }
    /// <summary>Pagina corrente.</summary>
    public int Page { get; set; }
    /// <summary>Dimensione pagina.</summary>
    public int PageSize { get; set; }
}

/// <summary>DTO di invito per creare un nuovo utente admin.</summary>
public class CreateAdminUserInviteDTO
{
    /// <summary>Email dell'invitato; obbligatoria e valida per inviare l'invito.</summary>
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    /// <summary>Nome dell'invitato; obbligatorio per il profilo iniziale.</summary>
    [Required]
    [MaxLength(100)]
    public string Nome { get; set; } = string.Empty;

    /// <summary>Cognome dell'invitato; obbligatorio per il profilo iniziale.</summary>
    [Required]
    [MaxLength(100)]
    public string Cognome { get; set; } = string.Empty;

    /// <summary>Ruolo da assegnare; la regex limita i valori ammessi.</summary>
    [Required]
    [RegularExpression("^(PowerUser|Admin)$")]
    public string Ruolo { get; set; } = string.Empty;
}

/// <summary>DTO di sicurezza dell'utente usato nelle schermate admin.</summary>
public class AdminUserSecurityDTO
{
    /// <summary>ID univoco dell'utente.</summary>
    public int Id { get; set; }
    /// <summary>Email dell'utente.</summary>
    public string Email { get; set; } = string.Empty;
    /// <summary>Ruolo dell'utente.</summary>
    public string Ruolo { get; set; } = string.Empty;
    /// <summary>Indica se è presente una password locale.</summary>
    public bool HasLocalPassword { get; set; }
    /// <summary>Indica se l'account è disabilitato.</summary>
    public bool IsDisabled { get; set; }
    /// <summary>Data di cambio password, se presente.</summary>
    public DateTime? PasswordChangedAtUtc { get; set; }
    /// <summary>Versione di autenticazione per invalidare token vecchi.</summary>
    public int AuthVersion { get; set; }
    /// <summary>Provider collegati all'account.</summary>
    public List<LinkedProviderInfoDTO> LinkedProviders { get; set; } = new();
}

/// <summary>Dettaglio di un provider collegato all'account.</summary>
public class LinkedProviderInfoDTO
{
    /// <summary>Nome tecnico del provider.</summary>
    public string Provider { get; set; } = string.Empty;
    /// <summary>Email usata al momento del login.</summary>
    public string EmailAtLogin { get; set; } = string.Empty;
    /// <summary>Data di collegamento del provider.</summary>
    public DateTime LinkedAtUtc { get; set; }
    /// <summary>Data dell'ultimo login, se disponibile.</summary>
    public DateTime? LastLoginAtUtc { get; set; }
}
