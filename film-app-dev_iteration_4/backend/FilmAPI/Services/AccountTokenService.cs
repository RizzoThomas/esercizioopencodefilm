using System.Security.Cryptography;
using System.Text;
using FilmAPI.Data;
using FilmAPI.Model;
using Microsoft.EntityFrameworkCore;

namespace FilmAPI.Services;

public class AccountTokenService : IAccountTokenService
{
    private readonly FilmDbContext _context;

    public AccountTokenService(FilmDbContext context)
    {
        _context = context;
    }

    public async Task<string> CreateTokenAsync(int userId, AccountActionTokenPurpose purpose, TimeSpan ttl, int? actorUserId = null, string? requestIp = null, string? userAgent = null)
    {
        var rawBytes = RandomNumberGenerator.GetBytes(32);
        var rawToken = Convert.ToBase64String(rawBytes)
            .Replace("/", "_").Replace("+", "-").TrimEnd('=');

        var tokenHash = HashToken(rawToken);

        await RevokeActiveTokensAsync(userId, purpose);

        var entity = new AccountActionToken
        {
            UserId = userId,
            Purpose = purpose,
            TokenHash = tokenHash,
            ExpiresAtUtc = DateTime.UtcNow.Add(ttl),
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = actorUserId,
            RequestIp = requestIp,
            UserAgent = userAgent
        };

        _context.AccountActionTokens.Add(entity);
        await _context.SaveChangesAsync();

        return rawToken;
    }

    public async Task<(bool valid, AccountActionToken? token)> ValidateTokenAsync(string rawToken, AccountActionTokenPurpose purpose)
    {
        var tokenHash = HashToken(rawToken);

        var token = await _context.AccountActionTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash && t.Purpose == purpose);

        if (token is null) return (false, null);
        if (token.UsedAtUtc is not null) return (false, null);
        if (token.RevokedAtUtc is not null) return (false, null);
        if (token.ExpiresAtUtc < DateTime.UtcNow) return (false, null);

        return (true, token);
    }

    public async Task<bool> ConsumeTokenAsync(string rawToken, AccountActionTokenPurpose purpose)
    {
        var (valid, token) = await ValidateTokenAsync(rawToken, purpose);
        if (!valid || token is null) return false;

        token.UsedAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task RevokeActiveTokensAsync(int userId, AccountActionTokenPurpose purpose)
    {
        var tokens = await _context.AccountActionTokens
            .Where(t => t.UserId == userId
                        && t.Purpose == purpose
                        && t.UsedAtUtc == null
                        && t.RevokedAtUtc == null
                        && t.ExpiresAtUtc > DateTime.UtcNow)
            .ToListAsync();

        foreach (var token in tokens)
        {
            token.RevokedAtUtc = DateTime.UtcNow;
        }

        if (tokens.Count > 0)
            await _context.SaveChangesAsync();
    }

    private static string HashToken(string rawToken)
    {
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToBase64String(hashBytes)
            .Replace("/", "_").Replace("+", "-").TrimEnd('=');
    }
}
