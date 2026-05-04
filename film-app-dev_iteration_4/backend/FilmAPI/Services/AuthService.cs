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

public class AuthService : IAuthService
{
    private readonly FilmDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ILogger<AuthService> _logger;
    private readonly string _jwtSecret;
    private readonly string _jwtIssuer;
    private readonly string _jwtAudience;
    private readonly int _accessTokenExpiryMinutes;
    private readonly int _refreshTokenExpiryDays;
    private const string DefaultDeviceId = "web-default";
    private const int TwoFactorTempTokenExpiryMinutes = 5;
    private const int TrustedDeviceExpiryDays = 3;
    private static readonly byte[] _tempTokenKey = RandomNumberGenerator.GetBytes(32);

    public AuthService(FilmDbContext context, IEmailService emailService, ILogger<AuthService> logger)
    {
        _context = context;
        _emailService = emailService;
        _logger = logger;
        _jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET") ?? "SuperSecretKeyForCineBaseJWTAuth2026!";
        _jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? "CineBaseAPI";
        _jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? "CineBaseWeb";
        _accessTokenExpiryMinutes = int.Parse(Environment.GetEnvironmentVariable("JWT_ACCESS_TOKEN_EXPIRY_MINUTES") ?? "60");
        _refreshTokenExpiryDays = int.Parse(Environment.GetEnvironmentVariable("JWT_REFRESH_TOKEN_EXPIRY_DAYS") ?? "7");
    }

    public async Task<AuthResponseDTO> RegisterAsync(RegisterRequestDTO dto)
    {
        var exists = await _context.Users.AnyAsync(u => u.Email == dto.Email);
        if (exists)
        {
            throw new InvalidOperationException("Email gia registrata");
        }

        var user = new User
        {
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Nome = dto.Nome,
            Cognome = dto.Cognome,
            Telefono = dto.Telefono,
            Ruolo = UserRole.User,
            DataRegistrazione = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

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

    public async Task<AuthResponseDTO> LoginAsync(LoginRequestDTO dto, HttpContext? httpContext = null)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (user is null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Credenziali non valide");
        }

        // Se 2FA è abilitato, verifica trusted device (header o cookie)
        if (user.TwoFactorEnabled && !string.IsNullOrEmpty(user.TwoFactorSecret))
        {
            var trustedDeviceToken = httpContext?.Request.Headers["X-Trusted-Device"].FirstOrDefault();
            var isTrusted = (!string.IsNullOrEmpty(trustedDeviceToken) && ValidateTrustedDeviceToken(trustedDeviceToken, user.Id))
                         || IsTrustedDevice(httpContext, user.Id);
            
            _logger.LogWarning("LoginAsync: utente {Email} ha 2FA abilitato. TrustedDevice={IsTrusted}",
                user.Email, isTrusted);
                
            if (!isTrusted)
            {
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

    public async Task<AuthResponseDTO> RefreshAsync(string refreshToken, string? deviceId)
    {
        var storedToken = await _context.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

        if (storedToken is null || !storedToken.IsActive)
        {
            throw new UnauthorizedAccessException("Refresh token non valido o scaduto");
        }

        var normalizedDeviceId = NormalizeDeviceId(deviceId);
        if (!string.Equals(storedToken.DeviceId, normalizedDeviceId, StringComparison.Ordinal))
        {
            throw new UnauthorizedAccessException("Refresh token non valido per questo device");
        }

        storedToken.RevokedAt = DateTime.UtcNow;

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

    public async Task<UserInfoDTO?> GetUserByIdAsync(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user is null) return null;

        return MapUserInfo(user);
    }

    private string GenerateAccessToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSecret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim("role", user.Ruolo.ToString()),
            new Claim("nome", user.Nome)
        };

        var token = new JwtSecurityToken(
            issuer: _jwtIssuer,
            audience: _jwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_accessTokenExpiryMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private async Task<RefreshToken> GenerateRefreshTokenAsync(int userId, string? deviceId)
    {
        var normalizedDeviceId = NormalizeDeviceId(deviceId);

        var activeTokensForDevice = await _context.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.DeviceId == normalizedDeviceId && rt.RevokedAt == null && rt.ExpiresAt > DateTime.UtcNow)
            .ToListAsync();

        foreach (var token in activeTokensForDevice)
        {
            token.RevokedAt = DateTime.UtcNow;
        }

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
    {
        return string.IsNullOrWhiteSpace(deviceId)
            ? DefaultDeviceId
            : deviceId.Trim();
    }

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

    public async Task<bool> ForgotPasswordAsync(string email)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user is null) return true; // Non rivelare se l'email esiste

        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace("/", "_").Replace("+", "-").TrimEnd('=');

        user.PasswordResetToken = token;
        user.ResetTokenExpiry = DateTime.UtcNow.AddHours(1);
        await _context.SaveChangesAsync();

        var frontendUrl = Environment.GetEnvironmentVariable("FRONTEND_URL") ?? "http://localhost:5001";
        var resetLink = $"{frontendUrl}/reset-password.html?token={Uri.EscapeDataString(token)}";

        await _emailService.SendPasswordResetAsync(user.Email, user.Nome, resetLink);

        return true;
    }

    public async Task<bool> ResetPasswordAsync(string token, string newPassword)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u =>
            u.PasswordResetToken == token &&
            u.ResetTokenExpiry != null &&
            u.ResetTokenExpiry > DateTime.UtcNow);

        if (user is null) return false;

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        user.PasswordResetToken = null;
        user.ResetTokenExpiry = null;
        await _context.SaveChangesAsync();

        return true;
    }

    // ═══════════════════ 2FA ══════════════════════════════════════════

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

    public async Task<bool> DisableTwoFactorAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId)
            ?? throw new InvalidOperationException("Utente non trovato");

        user.TwoFactorSecret = null;
        user.TwoFactorEnabled = false;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> VerifyTwoFactorCodeAsync(int userId, string code)
    {
        var user = await _context.Users.FindAsync(userId)
            ?? throw new InvalidOperationException("Utente non trovato");

        if (string.IsNullOrEmpty(user.TwoFactorSecret))
            return false;

        var secret = TotpUtility.FromBase32(user.TwoFactorSecret);
        return TotpUtility.VerifyCode(secret, code);
    }

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
        public int UserId { get; set; }
        public long Exp { get; set; }
    }

    // ─── Helper ──────────────────────────────────────────────────────

    // ─── Social Login ────────────────────────────────────────────────

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
