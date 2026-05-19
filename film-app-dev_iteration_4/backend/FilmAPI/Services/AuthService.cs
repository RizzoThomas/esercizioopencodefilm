// ============================================================================
// AuthService.cs — SERVIZIO DI AUTENTICAZIONE
// ============================================================================
// Questo servizio gestisce: registrazione, login, refresh token, logout,
// password reset, 2FA e social login.
//
// ARCHITETTURA JWT:
//   - Access Token: JWT con scadenza breve (60 min), firmato HMAC-SHA256
//   - Refresh Token: stringa random 64 byte, conservato nel DB, legato al device
//   - Device Identity: ogni dispositivo ha un UUID, il refresh è vincolato al device
//   - AuthVersion: se cambia, tutti i JWT emessi in precedenza diventano invalidi
// ============================================================================

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using FilmAPI.Data;
using FilmAPI.DTO;
using FilmAPI.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace FilmAPI.Services;

/// <summary>
/// Fornisce il servizio  per le operazioni di dominio esposte da questo modulo.
/// </summary>
/// <remarks>
/// Usato dai controller o endpoint che gestiscono le funzioni di . Dipendenze iniettate nel costruttore: nessuna dichiarata esplicitamente.
/// </remarks>
public class AuthService : IAuthService
{
    // ─── DEPENDENCY INJECTION ─────────────────────────────────────────────
    // Tutti i servizi vengono iniettati nel costruttore
    private readonly FilmDbContext _context;              // Database
    private readonly IEmailService _emailService;          // Invio email
    private readonly IAccountTokenService _accountTokenService;  // Token account
    private readonly IAccountEmailService _accountEmailService;  // Email account
    private readonly IUserSecurityAuditService _auditService;    // Log sicurezza
    private readonly ILogger<AuthService> _logger;

    // Parametri JWT letti da .env
    private readonly string _jwtSecret;
    private readonly string _jwtIssuer;
    private readonly string _jwtAudience;
    private readonly int _accessTokenExpiryMinutes;
    private readonly int _refreshTokenExpiryDays;

    private const string DefaultDeviceId = "web-default";
    private const int TwoFactorTempTokenExpiryMinutes = 5;
    private const int TrustedDeviceExpiryDays = 3;
    private static readonly byte[] _tempTokenKey = RandomNumberGenerator.GetBytes(32);

    /// <summary>
    /// Esegue l''operazione AuthService del servizio.
    /// </summary>
    /// <param name="context">Parametro necessario per l'operazione: context.</param>
    /// <param name="emailService">Parametro necessario per l'operazione: emailService.</param>
    /// <param name="accountTokenService">Parametro necessario per l'operazione: accountTokenService.</param>
    /// <param name="accountEmailService">Parametro necessario per l'operazione: accountEmailService.</param>
    /// <param name="auditService">Parametro necessario per l'operazione: auditService.</param>
    /// <param name="logger">Parametro necessario per l'operazione: logger.</param>
    /// <returns>Restituisce il risultato dell'operazione quando questa ha esito positivo; altrimenti il chiamante riceve un'eccezione o un risultato nullo/booleano secondo il contratto del metodo.</returns>
    /// <remarks>
    /// Effetti collaterali: scrive o aggiorna il database. può inviare email di notifica.
    /// </remarks>
    public AuthService(FilmDbContext context, IEmailService emailService, IAccountTokenService accountTokenService, IAccountEmailService accountEmailService, IUserSecurityAuditService auditService, ILogger<AuthService> logger)
    {
        _context = context;
        _emailService = emailService;
        _accountTokenService = accountTokenService;
        _accountEmailService = accountEmailService;
        _auditService = auditService;
        _logger = logger;
        // Legge la configurazione JWT dal .env con fallback per sviluppo
        _jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET") ?? "SuperSecretKeyForCineBaseJWTAuth2026!";
        _jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? "CineBaseAPI";
        _jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? "CineBaseWeb";
        _accessTokenExpiryMinutes = int.Parse(Environment.GetEnvironmentVariable("JWT_ACCESS_TOKEN_EXPIRY_MINUTES") ?? "60");
        _refreshTokenExpiryDays = int.Parse(Environment.GetEnvironmentVariable("JWT_REFRESH_TOKEN_EXPIRY_DAYS") ?? "7");
    }

    // ========================================================================
    // REGISTRAZIONE
    // ========================================================================
    // 1. Verifica che l'email non sia già registrata
    // 2. Crea User con PasswordHash = BCrypt(password)
    // 3. Genera AccessToken JWT
    // 4. Genera RefreshToken (con DeviceId)
    // ========================================================================
    /// <summary>
    /// Esegue l''operazione di business RegisterAsync del servizio.
    /// </summary>
    /// <param name="dto">Oggetto DTO di input necessario per eseguire l'operazione.</param>
    /// <returns>Restituisce in modo asincrono il risultato dell'operazione indicato dal tipo interno del Task quando la logica termina correttamente.</returns>
    /// <remarks>
    /// Effetti collaterali: scrive o aggiorna il database. può inviare email di notifica.
    /// </remarks>
    public async Task<AuthResponseDTO> RegisterAsync(RegisterRequestDTO dto)
    {
        // Controllo email duplicata
        var exists = await _context.Users.AnyAsync(u => u.Email == dto.Email);
        if (exists)
            throw new InvalidOperationException("Email gia registrata");

        // Crea nuovo utente con password hashata (BCrypt)
        // BCrypt è un algoritmo di hashing lento (resistente a brute force)
        var user = new User
        {
            Email = dto.Email,
            NormalizedEmail = dto.Email.Trim().ToUpperInvariant(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            LocalCredentialsEnabled = true,
            Nome = dto.Nome,
            Cognome = dto.Cognome,
            Telefono = dto.Telefono,
            Ruolo = UserRole.User,              // Default: utente base
            DataRegistrazione = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Genera JWT access token + refresh token
        var accessToken = GenerateAccessToken(user);
        var refreshToken = await GenerateRefreshTokenAsync(user.Id, dto.DeviceId);
        await _context.SaveChangesAsync();

        return new AuthResponseDTO
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken.Token,
            ExpiresAt = refreshToken.ExpiresAt,
            User = MapUserInfo(user)
        };
    }

    // ========================================================================
    // LOGIN
    // ========================================================================
    // 1. Cerca utente per email
    // 2. Verifica password con BCrypt.Verify()
    // 3. Controlla se account è disabilitato
    // 4. Se 2FA abilitato, richiede secondo fattore
    // 5. Genera JWT access token + refresh token
    // ========================================================================
    /// <summary>
    /// Esegue l''operazione di business LoginAsync del servizio.
    /// </summary>
    /// <param name="dto">Oggetto DTO di input necessario per eseguire l'operazione.</param>
    /// <param name="httpContext">Parametro necessario per l'operazione: httpContext.</param>
    /// <returns>Restituisce in modo asincrono il risultato dell'operazione indicato dal tipo interno del Task quando la logica termina correttamente.</returns>
    /// <remarks>
    /// Effetti collaterali: scrive o aggiorna il database. può inviare email di notifica.
    /// </remarks>
    public async Task<AuthResponseDTO> LoginAsync(LoginRequestDTO dto, HttpContext? httpContext = null)
    {
        // Cerca utente per email (esatta, non normalizzata)
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);

        // Verifica password con BCrypt
        // BCrypt.Verify confronta la password in chiaro con l'hash salvato
        if (user is null || user.PasswordHash is null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Credenziali non valide");

        // Account disabilitato? (admin può disabilitare)
        if (user.IsDisabled)
            throw new UnauthorizedAccessException("Account disabilitato");

        // Aggiorna timestamp ultimo login
        user.LastLoginAtUtc = DateTime.UtcNow;
        user.LastLoginProvider = "Local";

        // Se 2FA è abilitato, verifica trusted device (header o cookie)
        if (user.TwoFactorEnabled && !string.IsNullOrEmpty(user.TwoFactorSecret))
        {
            var trustedDeviceToken = httpContext?.Request.Headers["X-Trusted-Device"].FirstOrDefault();
            var isTrusted = (!string.IsNullOrEmpty(trustedDeviceToken) && ValidateTrustedDeviceToken(trustedDeviceToken, user.Id))
                         || IsTrustedDevice(httpContext, user.Id);
            
            if (!isTrusted)
            {
                // Richiede secondo fattore: restituisce temp token
                var tempToken = GenerateTwoFactorTempToken(user.Id);
                return new AuthResponseDTO
                {
                    RequiresTwoFactor = true,
                    TempToken = tempToken,
                    User = MapUserInfo(user)
                };
            }
        }

        return await GenerateAuthResponse(user, dto.DeviceId);
    }

    // ========================================================================
    // GENERAZIONE RISPOSTA AUTH (privato, chiamato da login/register)
    // ========================================================================
    private async Task<AuthResponseDTO> GenerateAuthResponse(User user, string? deviceId)
    {
        var accessToken = GenerateAccessToken(user);
        var refreshToken = await GenerateRefreshTokenAsync(user.Id, deviceId);
        await _context.SaveChangesAsync();

        return new AuthResponseDTO
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken.Token,
            ExpiresAt = refreshToken.ExpiresAt,
            User = MapUserInfo(user)
        };
    }

    // ========================================================================
    // REFRESH TOKEN
    // ========================================================================
    // 1. Cerca refresh token nel DB
    // 2. Verifica che sia attivo (non scaduto, non revocato)
    // 3. Verifica che il DeviceId corrisponda (device-aware)
    // 4. Revoca il vecchio token (ROTAZIONE)
    // 5. Genera NUOVO access token + NUOVO refresh token
    // ========================================================================
    /// <summary>
    /// Esegue l''operazione di business RefreshAsync del servizio.
    /// </summary>
    /// <param name="refreshToken">Token necessario per validare, rinnovare o revocare l'operazione richiesta.</param>
    /// <param name="deviceId">Identificativo necessario per individuare l'entità o il contesto di lavoro: deviceId.</param>
    /// <returns>Restituisce in modo asincrono il risultato dell'operazione indicato dal tipo interno del Task quando la logica termina correttamente.</returns>
    /// <remarks>
    /// Effetti collaterali: scrive o aggiorna il database. può inviare email di notifica.
    /// </remarks>
    public async Task<AuthResponseDTO> RefreshAsync(string refreshToken, string? deviceId)
    {
        var storedToken = await _context.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

        if (storedToken is null || !storedToken.IsActive)
            throw new UnauthorizedAccessException("Refresh token non valido o scaduto");

        // Device-aware: il token appartiene a questo dispositivo?
        var normalizedDeviceId = NormalizeDeviceId(deviceId);
        if (!string.Equals(storedToken.DeviceId, normalizedDeviceId, StringComparison.Ordinal))
            throw new UnauthorizedAccessException("Refresh token non valido per questo device");

        // ROTAZIONE: revoca il vecchio token
        storedToken.RevokedAt = DateTime.UtcNow;

        // Genera NUOVO token (rotation = più sicuro)
        var newRefreshToken = await GenerateRefreshTokenAsync(storedToken.UserId, normalizedDeviceId);
        var accessToken = GenerateAccessToken(storedToken.User!);

        await _context.SaveChangesAsync();

        return new AuthResponseDTO
        {
            AccessToken = accessToken,
            RefreshToken = newRefreshToken.Token,
            ExpiresAt = newRefreshToken.ExpiresAt,
            User = MapUserInfo(storedToken.User!)
        };
    }

    // ========================================================================
    // LOGOUT
    // ========================================================================
    // Revoca il refresh token (non lo elimina, lo marca come revocato)
    // ========================================================================
    /// <summary>
    /// Esegue l''operazione di business LogoutAsync del servizio.
    /// </summary>
    /// <param name="refreshToken">Token necessario per validare, rinnovare o revocare l'operazione richiesta.</param>
    /// <param name="deviceId">Identificativo necessario per individuare l'entità o il contesto di lavoro: deviceId.</param>
    /// <returns>Restituisce in modo asincrono il risultato dell'operazione indicato dal tipo interno del Task quando la logica termina correttamente.</returns>
    /// <remarks>
    /// Effetti collaterali: scrive o aggiorna il database. può inviare email di notifica.
    /// </remarks>
    public async Task<bool> LogoutAsync(string refreshToken, string? deviceId)
    {
        var normalizedDeviceId = NormalizeDeviceId(deviceId);
        var storedToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken && rt.DeviceId == normalizedDeviceId);

        if (storedToken is null) return false;

        storedToken.RevokedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Recupera o legge i dati tramite l''operazione GetUserByIdAsync del servizio.
    /// </summary>
    /// <param name="id">Identificativo necessario per individuare l'entità o il contesto di lavoro: id.</param>
    /// <returns>Restituisce in modo asincrono il risultato dell'operazione indicato dal tipo interno del Task quando la logica termina correttamente.</returns>
    /// <remarks>
    /// Effetti collaterali: scrive o aggiorna il database. può inviare email di notifica.
    /// </remarks>
    public async Task<UserInfoDTO?> GetUserByIdAsync(int id)
    {
        var user = await _context.Users.FindAsync(id);
        return user is null ? null : MapUserInfo(user);
    }

    // ========================================================================
    // GENERAZIONE ACCESS TOKEN (JWT)
    // ========================================================================
    // Crea un JWT firmato con HMAC-SHA256 contenente:
    //   - sub: User ID
    //   - email: Email utente
    //   - role: Ruolo (User/PowerUser/Admin) per RBAC
    //   - auth_version: per invalidare token dopo cambio password
    // ========================================================================
    private string GenerateAccessToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSecret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // Claims = informazioni sull'utente dentro il JWT
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),     // User ID
            new Claim(JwtRegisteredClaimNames.Email, user.Email),            // Email
            new Claim("role", user.Ruolo.ToString()),                        // Ruolo RBAC
            new Claim("nome", user.Nome),                                    // Nome
            new Claim("auth_version", user.AuthVersion.ToString())           // Versione sicurezza
        };

        var token = new JwtSecurityToken(
            issuer: _jwtIssuer,
            audience: _jwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_accessTokenExpiryMinutes),  // Scadenza
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    // ========================================================================
    // GENERAZIONE REFRESH TOKEN (con rotazione)
    // ========================================================================
    // 1. Normalizza DeviceId (default: "web-default")
    // 2. Revoca tutti i token attivi per questo (UserId, DeviceId)
    // 3. Crea nuovo token con stringa random 64 byte
    // ========================================================================
    private async Task<RefreshToken> GenerateRefreshTokenAsync(int userId, string? deviceId)
    {
        var normalizedDeviceId = NormalizeDeviceId(deviceId);

        // Revoca token attivi esistenti per questo device
        // Così ogni dispositivo ha UN SOLO refresh token attivo alla volta
        var activeTokensForDevice = await _context.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.DeviceId == normalizedDeviceId
                   && rt.RevokedAt == null && rt.ExpiresAt > DateTime.UtcNow)
            .ToListAsync();

        foreach (var token in activeTokensForDevice)
            token.RevokedAt = DateTime.UtcNow;

        // Genera nuovo refresh token (64 byte random = 512 bit)
        var refreshToken = new RefreshToken
        {
            Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
            UserId = userId,
            DeviceId = normalizedDeviceId,
            ExpiresAt = DateTime.UtcNow.AddDays(_refreshTokenExpiryDays),
            CreatedAt = DateTime.UtcNow
        };

        _context.RefreshTokens.Add(refreshToken);
        return refreshToken;
    }

    private static string NormalizeDeviceId(string? deviceId)
        => string.IsNullOrWhiteSpace(deviceId) ? DefaultDeviceId : deviceId.Trim();

    private static UserInfoDTO MapUserInfo(User user)
    {
        return new UserInfoDTO
        {
            Id = user.Id,
            Email = user.Email,
            Nome = user.Nome,
            Cognome = user.Cognome,
            Telefono = user.Telefono,
            Ruolo = user.Ruolo.ToString(),
            DataRegistrazione = user.DataRegistrazione,
            TwoFactorEnabled = user.TwoFactorEnabled
        };
    }

    // ═══════════════════ Password Reset ═══════════════════════════════

    /// <summary>
    /// Esegue l''operazione ForgotPasswordAsync del servizio.
    /// </summary>
    /// <param name="email">Indirizzo email usato per autenticazione, notifica o identificazione dell'utente.</param>
    /// <returns>Restituisce in modo asincrono il risultato dell'operazione indicato dal tipo interno del Task quando la logica termina correttamente.</returns>
    /// <remarks>
    /// Effetti collaterali: scrive o aggiorna il database. può inviare email di notifica.
    /// </remarks>
    public async Task<bool> ForgotPasswordAsync(string email)
    {
        var normalizedEmail = email.Trim().ToUpperInvariant();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail);
        if (user is null) return true;

        var ttlMinutes = int.TryParse(Environment.GetEnvironmentVariable("PASSWORD_RESET_TOKEN_TTL_MINUTES"), out var mins) ? mins : 30;
        var rawToken = await _accountTokenService.CreateTokenAsync(user.Id, AccountActionTokenPurpose.PasswordReset, TimeSpan.FromMinutes(ttlMinutes));

        var frontendBaseUrl = Environment.GetEnvironmentVariable("FRONTEND_BASE_URL") ?? "http://localhost:5001";
        var resetUrl = $"{frontendBaseUrl}/reset-password.html?token={Uri.EscapeDataString(rawToken)}";

        await _accountEmailService.SendPasswordResetAsync(user.Email, user.Nome, resetUrl);
        await _auditService.LogAsync(user.Id, null, "PasswordResetRequested");

        return true;
    }

    /// <summary>
    /// Esegue l''operazione ResetPasswordAsync del servizio.
    /// </summary>
    /// <param name="token">Token necessario per validare, rinnovare o revocare l'operazione richiesta.</param>
    /// <param name="newPassword">Nuova password da impostare dopo i controlli di sicurezza.</param>
    /// <returns>Restituisce in modo asincrono il risultato dell'operazione indicato dal tipo interno del Task quando la logica termina correttamente.</returns>
    /// <remarks>
    /// Effetti collaterali: scrive o aggiorna il database. può inviare email di notifica.
    /// </remarks>
    public async Task<bool> ResetPasswordAsync(string token, string newPassword)
    {
        var (valid, actionToken) = await _accountTokenService.ValidateTokenAsync(token, AccountActionTokenPurpose.PasswordReset);
        if (!valid || actionToken is null) return false;

        var consumed = await _accountTokenService.ConsumeTokenAsync(token, AccountActionTokenPurpose.PasswordReset);
        if (!consumed) return false;

        var user = await _context.Users.FindAsync(actionToken.UserId);
        if (user is null) return false;

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        user.LocalCredentialsEnabled = true;
        user.PasswordChangedAtUtc = DateTime.UtcNow;
        user.AuthVersion++;

        await RevokeAllRefreshTokensAsync(user.Id);
        await _auditService.LogAsync(user.Id, user.Id, "PasswordResetCompleted");
        await _context.SaveChangesAsync();

        return true;
    }

    // ═══════════════════ Change Password ══════════════════════════════

    /// <summary>
    /// Esegue l''operazione di business ChangePasswordAsync del servizio.
    /// </summary>
    /// <param name="userId">Identificativo necessario per individuare l'entità o il contesto di lavoro: userId.</param>
    /// <param name="currentPassword">Password attuale necessaria per verificare che la richiesta provenga dal titolare dell'account.</param>
    /// <param name="newPassword">Nuova password da impostare dopo i controlli di sicurezza.</param>
    /// <returns>Restituisce in modo asincrono il risultato dell'operazione indicato dal tipo interno del Task quando la logica termina correttamente.</returns>
    /// <remarks>
    /// Effetti collaterali: scrive o aggiorna il database. può inviare email di notifica.
    /// </remarks>
    public async Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user is null) return false;

        if (user.PasswordHash is null)
            return false;

        if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
            return false;

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        user.LocalCredentialsEnabled = true;
        user.PasswordChangedAtUtc = DateTime.UtcNow;
        user.AuthVersion++;

        await RevokeAllRefreshTokensAsync(user.Id);
        await _auditService.LogAsync(user.Id, userId, "PasswordChanged");
        await _context.SaveChangesAsync();

        await _accountEmailService.SendPasswordChangedAsync(user.Email, user.Nome);

        return true;
    }

    // ═══════════════════ Set Password (social-only → locale) ═════════

    /// <summary>
    /// Esegue l''operazione RequestSetPasswordAsync del servizio.
    /// </summary>
    /// <param name="userId">Identificativo necessario per individuare l'entità o il contesto di lavoro: userId.</param>
    /// <returns>Restituisce in modo asincrono il risultato dell'operazione indicato dal tipo interno del Task quando la logica termina correttamente.</returns>
    /// <remarks>
    /// Effetti collaterali: scrive o aggiorna il database. può inviare email di notifica.
    /// </remarks>
    public async Task<bool> RequestSetPasswordAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user is null) return false;

        var ttlMinutes = int.TryParse(Environment.GetEnvironmentVariable("SET_PASSWORD_TOKEN_TTL_MINUTES"), out var mins) ? mins : 60;
        var rawToken = await _accountTokenService.CreateTokenAsync(user.Id, AccountActionTokenPurpose.SetPassword, TimeSpan.FromMinutes(ttlMinutes));

        var frontendBaseUrl = Environment.GetEnvironmentVariable("FRONTEND_BASE_URL") ?? "http://localhost:5001";
        var setupUrl = $"{frontendBaseUrl}/set-password.html?token={Uri.EscapeDataString(rawToken)}";

        await _accountEmailService.SendSetPasswordAsync(user.Email, user.Nome, setupUrl);
        await _auditService.LogAsync(user.Id, userId, "SetPasswordRequested");

        return true;
    }

    // ═══════════════════ Account Security ═════════════════════════════

    /// <summary>
    /// Recupera o legge i dati tramite l''operazione GetAccountSecurityAsync del servizio.
    /// </summary>
    /// <param name="userId">Identificativo necessario per individuare l'entità o il contesto di lavoro: userId.</param>
    /// <returns>Restituisce in modo asincrono il risultato dell'operazione indicato dal tipo interno del Task quando la logica termina correttamente.</returns>
    /// <remarks>
    /// Effetti collaterali: scrive o aggiorna il database. può inviare email di notifica.
    /// </remarks>
    public async Task<AccountSecurityDTO?> GetAccountSecurityAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user is null) return null;

        var linkedProviders = await _context.UserExternalLogins
            .Where(el => el.UserId == userId && el.RevokedAtUtc == null)
            .ToListAsync();

        return new AccountSecurityDTO
        {
            HasLocalPassword = !string.IsNullOrEmpty(user.PasswordHash),
            PasswordChangedAtUtc = user.PasswordChangedAtUtc,
            LinkedProviders = linkedProviders.Select(el => new ExternalProviderDTO
            {
                Provider = el.Provider.ToString(),
                Name = el.EmailAtLogin,
                StartUrl = string.Empty
            }).ToList(),
            AuthVersion = user.AuthVersion
        };
    }

    // ═══════════════════ Revoke Refresh Tokens ════════════════════════

    private async Task RevokeAllRefreshTokensAsync(int userId)
    {
        var tokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.RevokedAt == null)
            .ToListAsync();

        foreach (var t in tokens)
            t.RevokedAt = DateTime.UtcNow;
    }

    // ═══════════════════ 2FA ══════════════════════════════════════════

    /// <summary>
    /// Esegue l''operazione di business GenerateTwoFactorSetupAsync del servizio.
    /// </summary>
    /// <param name="userId">Identificativo necessario per individuare l'entità o il contesto di lavoro: userId.</param>
    /// <returns>Restituisce in modo asincrono il risultato dell'operazione indicato dal tipo interno del Task quando la logica termina correttamente.</returns>
    /// <remarks>
    /// Effetti collaterali: scrive o aggiorna il database. può inviare email di notifica.
    /// </remarks>
    public async Task<TwoFactorSetupResponseDTO> GenerateTwoFactorSetupAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId)
            ?? throw new InvalidOperationException("Utente non trovato");

        var secret = TotpUtility.GenerateSecret();
        var base32 = TotpUtility.ToBase32(secret);

        user.TwoFactorSecret = base32;
        user.TwoFactorEnabled = false;
        await _context.SaveChangesAsync();

        var qrUri = TotpUtility.GetQrCodeUri(user.Email, secret);

        // Genera QR code come Base64 PNG
        string qrBase64;
        using (var qrGenerator = new QRCoder.QRCodeGenerator())
        using (var qrData = qrGenerator.CreateQrCode(qrUri, QRCoder.QRCodeGenerator.ECCLevel.Q))
        using (var qr = new QRCoder.PngByteQRCode(qrData))
        {
            qrBase64 = Convert.ToBase64String(qr.GetGraphic(6));
        }

        return new TwoFactorSetupResponseDTO
        {
            Secret = base32,
            QrCodeBase64 = $"data:image/png;base64,{qrBase64}",
            ManualKey = FormatManualKey(base32)
        };
    }

    /// <summary>
    /// Esegue l''operazione EnableTwoFactorAsync del servizio.
    /// </summary>
    /// <param name="userId">Identificativo necessario per individuare l'entità o il contesto di lavoro: userId.</param>
    /// <param name="code">Parametro necessario per l'operazione: code.</param>
    /// <returns>Restituisce in modo asincrono il risultato dell'operazione indicato dal tipo interno del Task quando la logica termina correttamente.</returns>
    /// <remarks>
    /// Effetti collaterali: scrive o aggiorna il database.
    /// </remarks>
    public async Task<bool> EnableTwoFactorAsync(int userId, string code)
    {
        var user = await _context.Users.FindAsync(userId)
            ?? throw new InvalidOperationException("Utente non trovato");

        if (string.IsNullOrEmpty(user.TwoFactorSecret))
            throw new InvalidOperationException("2FA non ancora configurato");

        var secret = TotpUtility.FromBase32(user.TwoFactorSecret);
        if (!TotpUtility.VerifyCode(secret, code))
            return false;

        user.TwoFactorEnabled = true;
        await _context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Esegue l''operazione DisableTwoFactorAsync del servizio.
    /// </summary>
    /// <param name="userId">Identificativo necessario per individuare l'entità o il contesto di lavoro: userId.</param>
    /// <returns>Restituisce in modo asincrono il risultato dell'operazione indicato dal tipo interno del Task quando la logica termina correttamente.</returns>
    /// <remarks>
    /// Effetti collaterali: scrive o aggiorna il database.
    /// </remarks>
    public async Task<bool> DisableTwoFactorAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId)
            ?? throw new InvalidOperationException("Utente non trovato");

        user.TwoFactorSecret = null;
        user.TwoFactorEnabled = false;
        await _context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Esegue l''operazione VerifyTwoFactorCodeAsync del servizio.
    /// </summary>
    /// <param name="userId">Identificativo necessario per individuare l'entità o il contesto di lavoro: userId.</param>
    /// <param name="code">Parametro necessario per l'operazione: code.</param>
    /// <returns>Restituisce in modo asincrono il risultato dell'operazione indicato dal tipo interno del Task quando la logica termina correttamente.</returns>
    /// <remarks>
    /// Effetti collaterali: non introduce effetti collaterali esterni evidenti oltre alla logica di lettura o validazione.
    /// </remarks>
    public async Task<bool> VerifyTwoFactorCodeAsync(int userId, string code)
    {
        var user = await _context.Users.FindAsync(userId)
            ?? throw new InvalidOperationException("Utente non trovato");

        if (string.IsNullOrEmpty(user.TwoFactorSecret))
            return false;

        var secret = TotpUtility.FromBase32(user.TwoFactorSecret);
        return TotpUtility.VerifyCode(secret, code);
    }

    /// <summary>
    /// Esegue l''operazione di business LoginWith2FaAsync del servizio.
    /// </summary>
    /// <param name="tempToken">Token necessario per validare, rinnovare o revocare l'operazione richiesta.</param>
    /// <param name="code">Parametro necessario per l'operazione: code.</param>
    /// <param name="trustDevice">Parametro necessario per l'operazione: trustDevice.</param>
    /// <param name="deviceId">Identificativo necessario per individuare l'entità o il contesto di lavoro: deviceId.</param>
    /// <param name="httpContext">Parametro necessario per l'operazione: httpContext.</param>
    /// <returns>Restituisce in modo asincrono il risultato dell'operazione indicato dal tipo interno del Task quando la logica termina correttamente.</returns>
    /// <remarks>
    /// Effetti collaterali: non introduce effetti collaterali esterni evidenti oltre alla logica di lettura o validazione.
    /// </remarks>
    public async Task<AuthResponseDTO> LoginWith2FaAsync(string tempToken, string code, bool trustDevice, string? deviceId, HttpContext? httpContext = null)
    {
        // Decodifica temp token
        var parts = tempToken.Split('.');
        if (parts.Length != 2)
            throw new UnauthorizedAccessException("Token 2FA non valido");

        var payload = parts[0];
        var signature = parts[1];

        // Verifica firma HMAC
        using var hmac = new HMACSHA256(_tempTokenKey);
        var computedSig = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload)))
            .Replace("/", "_").Replace("+", "-").TrimEnd('=');

        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(signature),
                Encoding.UTF8.GetBytes(computedSig)))
            throw new UnauthorizedAccessException("Token 2FA non valido");

        // Decodifica payload (Base64 URL-safe)
        var json = Encoding.UTF8.GetString(Convert.FromBase64String(
            payload.Replace("-", "+").Replace("_", "/") + new string('=', (4 - payload.Length % 4) % 4)));

        var payloadObj = System.Text.Json.JsonSerializer.Deserialize<TempTokenPayload>(json);
        if (payloadObj == null || payloadObj.Exp < DateTimeOffset.UtcNow.ToUnixTimeSeconds())
            throw new UnauthorizedAccessException("Token 2FA scaduto");

        var user = await _context.Users.FindAsync(payloadObj.UserId)
            ?? throw new UnauthorizedAccessException("Utente non trovato");

        if (!user.TwoFactorEnabled || string.IsNullOrEmpty(user.TwoFactorSecret))
            throw new InvalidOperationException("2FA non abilitato");

        var secret = TotpUtility.FromBase32(user.TwoFactorSecret);
        if (!TotpUtility.VerifyCode(secret, code))
            throw new UnauthorizedAccessException("Codice 2FA non valido");

        // Salva trusted device
        string? trustedDeviceToken = null;
        if (trustDevice)
            trustedDeviceToken = GenerateTrustedDeviceToken(user.Id);

        var auth = await GenerateAuthResponse(user, deviceId);
        auth.TrustedDeviceToken = trustedDeviceToken;
        return auth;
    }

    private string GenerateTrustedDeviceToken(int userId)
    {
        var exp = DateTimeOffset.UtcNow.AddDays(TrustedDeviceExpiryDays).ToUnixTimeSeconds();
        var payload = $"{userId}:{exp}";
        var signature = Convert.ToBase64String(
            new HMACSHA256(_tempTokenKey).ComputeHash(Encoding.UTF8.GetBytes(payload)))
            .Replace("/", "_").Replace("+", "-").TrimEnd('=');
        return $"{payload}:{signature}";
    }

    private bool ValidateTrustedDeviceToken(string token, int userId)
    {
        var parts = token.Split(':');
        if (parts.Length != 3) return false;
        if (!int.TryParse(parts[0], out var tokenUserId) || tokenUserId != userId) return false;
        if (!long.TryParse(parts[1], out var expiryUnix)) return false;
        if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() > expiryUnix) return false;

        var message = $"{parts[0]}:{parts[1]}";
        var expectedSig = Convert.ToBase64String(
            new HMACSHA256(_tempTokenKey).ComputeHash(Encoding.UTF8.GetBytes(message)))
            .Replace("/", "_").Replace("+", "-").TrimEnd('=');

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(parts[2]),
            Encoding.UTF8.GetBytes(expectedSig));
    }

    // ─── Trusted Device ──────────────────────────────────────────────

    private bool IsTrustedDevice(HttpContext? httpContext, int userId)
    {
        if (httpContext == null) return false;

        var cookie = httpContext.Request.Cookies["cb_trusted_device"];
        if (string.IsNullOrEmpty(cookie)) return false;

        var parts = cookie.Split(':');
        if (parts.Length != 3) return false;

        if (!int.TryParse(parts[0], out var cookieUserId) || cookieUserId != userId)
            return false;

        if (!long.TryParse(parts[1], out var expiryUnix))
            return false;

        if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() > expiryUnix)
            return false;

        // Verifica firma HMAC
        var message = $"{parts[0]}:{parts[1]}";
        var expectedSig = Convert.ToBase64String(
            new HMACSHA256(_tempTokenKey).ComputeHash(Encoding.UTF8.GetBytes(message)))
            .Replace("/", "_").Replace("+", "-").TrimEnd('=');

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(parts[2]),
            Encoding.UTF8.GetBytes(expectedSig));
    }

    private static void SetTrustedDeviceCookie(HttpContext httpContext, int userId)
    {
        var expiry = DateTimeOffset.UtcNow.AddDays(TrustedDeviceExpiryDays);
        var expiryUnix = expiry.ToUnixTimeSeconds();
        var message = $"{userId}:{expiryUnix}";
        var signature = Convert.ToBase64String(
            new HMACSHA256(_tempTokenKey).ComputeHash(Encoding.UTF8.GetBytes(message)))
            .Replace("/", "_").Replace("+", "-").TrimEnd('=');

        httpContext.Response.Cookies.Append("cb_trusted_device", $"{message}:{signature}", new CookieOptions
        {
            HttpOnly = true,
            Secure = false,
            SameSite = SameSiteMode.None,
            Expires = expiry
        });
    }

    // ─── Temp Token 2FA ──────────────────────────────────────────────

    private static string GenerateTwoFactorTempToken(int userId)
    {
        var exp = DateTimeOffset.UtcNow.AddMinutes(TwoFactorTempTokenExpiryMinutes).ToUnixTimeSeconds();
        var payload = System.Text.Json.JsonSerializer.Serialize(new TempTokenPayload
        {
            UserId = userId,
            Exp = exp
        });

        var payloadBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(payload))
            .Replace("/", "_").Replace("+", "-").TrimEnd('=');

        using var hmac = new HMACSHA256(_tempTokenKey);
        var signature = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(payloadBase64)))
            .Replace("/", "_").Replace("+", "-").TrimEnd('=');

        return $"{payloadBase64}.{signature}";
    }

    private class TempTokenPayload
    {
        /// <summary>
        /// Rappresenta la dipendenza o il dato esposto tramite la proprietà UserId.
        /// </summary>
        /// <remarks>
        /// Serve al servizio per completare le sue operazioni di lettura, validazione, persistenza o integrazione esterna.
        /// </remarks>
        public int UserId { get; set; }
        /// <summary>
        /// Rappresenta la dipendenza o il dato esposto tramite la proprietà Exp.
        /// </summary>
        /// <remarks>
        /// Serve al servizio per completare le sue operazioni di lettura, validazione, persistenza o integrazione esterna.
        /// </remarks>
        public long Exp { get; set; }
    }

    // ─── Helper ──────────────────────────────────────────────────────

    // ─── Social Login ────────────────────────────────────────────────

    /// <summary>
    /// Esegue l''operazione SocialLoginAsync del servizio.
    /// </summary>
    /// <param name="user">Parametro necessario per l'operazione: user.</param>
    /// <param name="deviceId">Identificativo necessario per individuare l'entità o il contesto di lavoro: deviceId.</param>
    /// <returns>Restituisce in modo asincrono il risultato dell'operazione indicato dal tipo interno del Task quando la logica termina correttamente.</returns>
    /// <remarks>
    /// Effetti collaterali: scrive o aggiorna il database.
    /// </remarks>
    public async Task<AuthResponseDTO> SocialLoginAsync(User user, string? deviceId = null)
    {
        return await GenerateAuthResponse(user, deviceId);
    }

    // ─── Helper ──────────────────────────────────────────────────────

    private static string FormatManualKey(string base32)
    {
        var chunks = new List<string>();
        for (var i = 0; i < base32.Length; i += 4)
            chunks.Add(i + 4 <= base32.Length ? base32.Substring(i, 4) : base32.Substring(i));
        return string.Join(" ", chunks);
    }
}
