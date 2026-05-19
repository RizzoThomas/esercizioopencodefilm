using System.Text.Json;
using FilmAPI.Data;
using FilmAPI.DTO;
using FilmAPI.Model;
using Microsoft.EntityFrameworkCore;

namespace FilmAPI.Services;

/// <summary>
/// Fornisce il servizio  per le operazioni di dominio esposte da questo modulo.
/// </summary>
/// <remarks>
/// Usato dai controller o endpoint che gestiscono le funzioni di . Dipendenze iniettate nel costruttore: nessuna dichiarata esplicitamente.
/// </remarks>
public class UserAdminService : IUserAdminService
{
    private readonly FilmDbContext _context;
    private readonly IAccountTokenService _accountTokenService;
    private readonly IAccountEmailService _accountEmailService;
    private readonly IUserSecurityAuditService _userSecurityAuditService;

    private const int AdminInviteTokenTtlHours = 24;
    private const int SetPasswordTokenTtlMinutes = 60;

    public UserAdminService(FilmDbContext context, IAccountTokenService accountTokenService,
        IAccountEmailService accountEmailService, IUserSecurityAuditService userSecurityAuditService)
    {
        _context = context;
        _accountTokenService = accountTokenService;
        _accountEmailService = accountEmailService;
        _userSecurityAuditService = userSecurityAuditService;
    }

    /// <summary>
    /// Recupera o legge i dati tramite l''operazione GetAllUsersAsync del servizio.
    /// </summary>
    /// <returns>Restituisce in modo asincrono il risultato dell'operazione indicato dal tipo interno del Task quando la logica termina correttamente.</returns>
    /// <remarks>
    /// Effetti collaterali: scrive o aggiorna il database. può inviare email di notifica.
    /// </remarks>
    public async Task<List<UserAdminDTO>> GetAllUsersAsync()
    {
        return await _context.Users
            .Select(u => new UserAdminDTO
            {
                Id = u.Id,
                Email = u.Email,
                Nome = u.Nome,
                Cognome = u.Cognome,
                Telefono = u.Telefono,
                Ruolo = u.Ruolo.ToString(),
                DataRegistrazione = u.DataRegistrazione,
                CreditoResiduo = u.CreditoResiduo
            })
            .ToListAsync();
    }

    /// <summary>
    /// Recupera o legge i dati tramite l''operazione GetUsersPagedAsync del servizio.
    /// </summary>
    /// <param name="page">Parametro necessario per l'operazione: page.</param>
    /// <param name="pageSize">Parametro necessario per l'operazione: pageSize.</param>
    /// <param name="search">Parametro necessario per l'operazione: search.</param>
    /// <param name="role">Parametro necessario per l'operazione: role.</param>
    /// <returns>Restituisce in modo asincrono il risultato dell'operazione indicato dal tipo interno del Task quando la logica termina correttamente.</returns>
    /// <remarks>
    /// Effetti collaterali: scrive o aggiorna il database. può inviare email di notifica.
    /// </remarks>
    public async Task<AdminUserPagedResultDTO> GetUsersPagedAsync(int page, int pageSize, string? search, string? role)
    {
        var query = _context.Users
            .Include(u => u.ExternalLogins)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(u => u.Email.Contains(term) || u.Nome.Contains(term) || u.Cognome.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(role) && Enum.TryParse<UserRole>(role, out var roleEnum))
        {
            query = query.Where(u => u.Ruolo == roleEnum);
        }

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(u => u.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new AdminUserListItemDTO
            {
                Id = u.Id,
                Email = u.Email,
                Nome = u.Nome,
                Cognome = u.Cognome,
                Ruolo = u.Ruolo.ToString(),
                CreditoResiduo = u.CreditoResiduo,
                LocalCredentialsEnabled = u.LocalCredentialsEnabled,
                IsDisabled = u.IsDisabled,
                LinkedProviders = u.ExternalLogins
                    .Where(el => el.RevokedAtUtc == null)
                    .Select(el => el.Provider.ToString())
                    .ToList(),
                DataRegistrazione = u.DataRegistrazione,
                LastLoginAtUtc = u.LastLoginAtUtc
            })
            .ToListAsync();

        return new AdminUserPagedResultDTO
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize
        };
    }

    /// <summary>
    /// Esegue l''operazione di business CreateInviteAsync del servizio.
    /// </summary>
    /// <param name="dto">Oggetto DTO di input necessario per eseguire l'operazione.</param>
    /// <param name="adminUserId">Identificativo necessario per individuare l'entità o il contesto di lavoro: adminUserId.</param>
    /// <returns>Restituisce in modo asincrono il risultato dell'operazione indicato dal tipo interno del Task quando la logica termina correttamente.</returns>
    /// <remarks>
    /// Effetti collaterali: scrive o aggiorna il database. può inviare email di notifica.
    /// </remarks>
    public async Task<AdminUserListItemDTO?> CreateInviteAsync(CreateAdminUserInviteDTO dto, int adminUserId)
    {
        var normalizedEmail = dto.Email.Trim().ToLowerInvariant();

        var existing = await _context.Users
            .FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail);
        if (existing is not null) return null;

        if (!Enum.TryParse<UserRole>(dto.Ruolo, out var role))
        {
            throw new InvalidOperationException("Ruolo non valido. Usare 'PowerUser' o 'Admin'.");
        }

        if (role == UserRole.User)
        {
            throw new InvalidOperationException("Non e possibile invitare un utente con ruolo 'User'.");
        }

        var user = new User
        {
            Email = dto.Email.Trim(),
            NormalizedEmail = normalizedEmail,
            Nome = dto.Nome.Trim(),
            Cognome = dto.Cognome.Trim(),
            Ruolo = role,
            PasswordHash = null,
            LocalCredentialsEnabled = false,
            MustChangePassword = true,
            IsDisabled = true,
            DataRegistrazione = DateTime.UtcNow,
            AuthVersion = 1,
            CreditoResiduo = 0
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var rawToken = await _accountTokenService.CreateTokenAsync(
            user.Id, AccountActionTokenPurpose.AdminInvite,
            TimeSpan.FromHours(AdminInviteTokenTtlHours), adminUserId);

        var frontendBaseUrl = Environment.GetEnvironmentVariable("FRONTEND_BASE_URL") ?? "http://localhost:5001";
        var inviteUrl = $"{frontendBaseUrl}/set-password.html?token={Uri.EscapeDataString(rawToken)}";

        await _accountEmailService.SendAdminInviteAsync(user.Email, user.Nome, dto.Ruolo, inviteUrl);

        await _userSecurityAuditService.LogAsync(
            user.Id, adminUserId, "AdminInviteCreated",
            metadataJson: JsonSerializer.Serialize(new { role = dto.Ruolo }));

        return MapToListItem(user);
    }

    /// <summary>
    /// Esegue l''operazione di business SendPasswordSetupAsync del servizio.
    /// </summary>
    /// <param name="userId">Identificativo necessario per individuare l'entità o il contesto di lavoro: userId.</param>
    /// <param name="adminUserId">Identificativo necessario per individuare l'entità o il contesto di lavoro: adminUserId.</param>
    /// <returns>Restituisce in modo asincrono il risultato dell'operazione indicato dal tipo interno del Task quando la logica termina correttamente.</returns>
    /// <remarks>
    /// Effetti collaterali: scrive o aggiorna il database. può inviare email di notifica.
    /// </remarks>
    public async Task<bool> SendPasswordSetupAsync(int userId, int adminUserId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user is null) return false;

        var rawToken = await _accountTokenService.CreateTokenAsync(
            user.Id, AccountActionTokenPurpose.SetPassword,
            TimeSpan.FromMinutes(SetPasswordTokenTtlMinutes), adminUserId);

        var frontendBaseUrl = Environment.GetEnvironmentVariable("FRONTEND_BASE_URL") ?? "http://localhost:5001";
        var setupUrl = $"{frontendBaseUrl}/set-password.html?token={Uri.EscapeDataString(rawToken)}";

        await _accountEmailService.SendSetPasswordAsync(user.Email, user.Nome, setupUrl);

        await _userSecurityAuditService.LogAsync(
            user.Id, adminUserId, "PasswordSetupRequested");

        return true;
    }

    /// <summary>
    /// Esegue l''operazione di business UpdateUserRoleAsync del servizio.
    /// </summary>
    /// <param name="userId">Identificativo necessario per individuare l'entità o il contesto di lavoro: userId.</param>
    /// <param name="dto">Oggetto DTO di input necessario per eseguire l'operazione.</param>
    /// <param name="requestingUserId">Identificativo necessario per individuare l'entità o il contesto di lavoro: requestingUserId.</param>
    /// <returns>Restituisce in modo asincrono il risultato dell'operazione indicato dal tipo interno del Task quando la logica termina correttamente.</returns>
    /// <remarks>
    /// Effetti collaterali: scrive o aggiorna il database. può inviare email di notifica.
    /// </remarks>
    public async Task<UserAdminDTO?> UpdateUserRoleAsync(int userId, UpdateRuoloDTO dto, int requestingUserId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user is null) return null;

        if (!Enum.TryParse<UserRole>(dto.NuovoRuolo, out var newRole))
        {
            throw new InvalidOperationException("Ruolo non valido");
        }

        if (user.Ruolo == newRole)
            return MapToDTO(user);

        if (!user.LocalCredentialsEnabled && (newRole == UserRole.PowerUser || newRole == UserRole.Admin))
        {
            throw new InvalidOperationException("social_only_no_password");
        }

        if (user.Ruolo == UserRole.Admin && newRole != UserRole.Admin)
        {
            var adminCount = await _context.Users.CountAsync(u => u.Ruolo == UserRole.Admin);
            if (adminCount <= 1)
            {
                throw new InvalidOperationException("Non e possibile degradare l'ultimo admin");
            }
        }

        var oldRole = user.Ruolo.ToString();

        user.Ruolo = newRole;
        user.AuthVersion++;

        var activeTokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.RevokedAt == null && rt.ExpiresAt > DateTime.UtcNow)
            .ToListAsync();
        foreach (var token in activeTokens)
        {
            token.RevokedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        await _userSecurityAuditService.LogAsync(
            user.Id, requestingUserId, "RoleChanged",
            metadataJson: JsonSerializer.Serialize(new { oldRole, newRole = newRole.ToString() }));

        return MapToDTO(user);
    }

    /// <summary>
    /// Esegue l''operazione di business UpdateUserCreditoAsync del servizio.
    /// </summary>
    /// <param name="userId">Identificativo necessario per individuare l'entità o il contesto di lavoro: userId.</param>
    /// <param name="dto">Oggetto DTO di input necessario per eseguire l'operazione.</param>
    /// <returns>Restituisce in modo asincrono il risultato dell'operazione indicato dal tipo interno del Task quando la logica termina correttamente.</returns>
    /// <remarks>
    /// Effetti collaterali: scrive o aggiorna il database. può inviare email di notifica.
    /// </remarks>
    public async Task<UserAdminDTO?> UpdateUserCreditoAsync(int userId, UpdateCreditoDTO dto)
    {
        if (dto.NuovoCredito < 0)
            throw new InvalidOperationException("Il credito non puo essere negativo.");

        var user = await _context.Users.FindAsync(userId);
        if (user is null) return null;

        user.CreditoResiduo = dto.NuovoCredito;
        await _context.SaveChangesAsync();

        return MapToDTO(user);
    }

    /// <summary>
    /// Recupera o legge i dati tramite l''operazione GetUserSecurityAsync del servizio.
    /// </summary>
    /// <param name="userId">Identificativo necessario per individuare l'entità o il contesto di lavoro: userId.</param>
    /// <returns>Restituisce in modo asincrono il risultato dell'operazione indicato dal tipo interno del Task quando la logica termina correttamente.</returns>
    /// <remarks>
    /// Effetti collaterali: può inviare email di notifica.
    /// </remarks>
    public async Task<AdminUserSecurityDTO?> GetUserSecurityAsync(int userId)
    {
        var user = await _context.Users
            .Include(u => u.ExternalLogins)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user is null) return null;

        return new AdminUserSecurityDTO
        {
            Id = user.Id,
            Email = user.Email,
            Ruolo = user.Ruolo.ToString(),
            HasLocalPassword = !string.IsNullOrWhiteSpace(user.PasswordHash),
            IsDisabled = user.IsDisabled,
            PasswordChangedAtUtc = user.PasswordChangedAtUtc,
            AuthVersion = user.AuthVersion,
            LinkedProviders = user.ExternalLogins
                .Select(el => new LinkedProviderInfoDTO
                {
                    Provider = el.Provider.ToString(),
                    EmailAtLogin = el.EmailAtLogin,
                    LinkedAtUtc = el.LinkedAtUtc,
                    LastLoginAtUtc = el.LastLoginAtUtc
                })
                .ToList()
        };
    }

    private static UserAdminDTO MapToDTO(User user)
    {
        return new UserAdminDTO
        {
            Id = user.Id,
            Email = user.Email,
            Nome = user.Nome,
            Cognome = user.Cognome,
            Telefono = user.Telefono,
            Ruolo = user.Ruolo.ToString(),
            DataRegistrazione = user.DataRegistrazione,
            CreditoResiduo = user.CreditoResiduo
        };
    }

    private static AdminUserListItemDTO MapToListItem(User user)
    {
        return new AdminUserListItemDTO
        {
            Id = user.Id,
            Email = user.Email,
            Nome = user.Nome,
            Cognome = user.Cognome,
            Ruolo = user.Ruolo.ToString(),
            CreditoResiduo = user.CreditoResiduo,
            LocalCredentialsEnabled = user.LocalCredentialsEnabled,
            IsDisabled = user.IsDisabled,
            LinkedProviders = new List<string>(),
            DataRegistrazione = user.DataRegistrazione,
            LastLoginAtUtc = user.LastLoginAtUtc
        };
    }
}
