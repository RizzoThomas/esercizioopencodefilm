namespace FilmAPI.Services;
using FilmAPI.Model;

public interface IAccountTokenService
{
    Task<string> CreateTokenAsync(int userId, AccountActionTokenPurpose purpose, TimeSpan ttl, int? actorUserId = null, string? requestIp = null, string? userAgent = null);
    Task<(bool valid, AccountActionToken? token)> ValidateTokenAsync(string rawToken, AccountActionTokenPurpose purpose);
    Task<bool> ConsumeTokenAsync(string rawToken, AccountActionTokenPurpose purpose);
    Task RevokeActiveTokensAsync(int userId, AccountActionTokenPurpose purpose);
}
