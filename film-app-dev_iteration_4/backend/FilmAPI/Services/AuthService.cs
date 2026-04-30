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
    private readonly string _jwtSecret;
    private readonly string _jwtIssuer;
    private readonly string _jwtAudience;
    private readonly int _accessTokenExpiryMinutes;
    private readonly int _refreshTokenExpiryDays;
    private const string DefaultDeviceId = "web-default";

    public AuthService(FilmDbContext context)
    {
        _context = context;
        _jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET") ?? "SuperSecretKeyForCineBaseJWTAuth2026!";
        _jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? "CineBaseAPI";
        _jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? "CineBaseWeb";
        _accessTokenExpiryMinutes = int.Parse(Environment.GetEnvironmentVariable("JWT_ACCESS_TOKEN_EXPIRY_MINUTES") ?? "15");
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

    public async Task<AuthResponseDTO> LoginAsync(LoginRequestDTO dto)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (user is null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Credenziali non valide");
        }

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
            DataRegistrazione = user.DataRegistrazione
        };
    }
}
