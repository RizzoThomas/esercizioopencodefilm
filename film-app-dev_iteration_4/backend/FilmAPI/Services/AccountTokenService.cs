using System.Security.Cryptography;
using System.Text;
using FilmAPI.Data;
using FilmAPI.Model;
using Microsoft.EntityFrameworkCore;

namespace FilmAPI.Services;

/// <summary>
/// Fornisce il servizio  per le operazioni di dominio esposte da questo modulo.
/// </summary>
/// <remarks>
/// Usato dai controller o endpoint che gestiscono le funzioni di . Dipendenze iniettate nel costruttore: nessuna dichiarata esplicitamente.
/// </remarks>
public class AccountTokenService : IAccountTokenService
{
    private readonly FilmDbContext _context;

    /// <summary>
    /// Esegue l''operazione AccountTokenService del servizio.
    /// </summary>
    /// <param name="context">Parametro necessario per l'operazione: context.</param>
    /// <returns>Restituisce il risultato dell'operazione quando questa ha esito positivo; altrimenti il chiamante riceve un'eccezione o un risultato nullo/booleano secondo il contratto del metodo.</returns>
    /// <remarks>
    /// Effetti collaterali: scrive o aggiorna il database.
    /// </remarks>
    public AccountTokenService(FilmDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Esegue l''operazione di business CreateTokenAsync del servizio.
    /// </summary>
    /// <param name="userId">Identificativo necessario per individuare l'entità o il contesto di lavoro: userId.</param>
    /// <param name="purpose">Parametro necessario per l'operazione: purpose.</param>
    /// <param name="ttl">Parametro necessario per l'operazione: ttl.</param>
    /// <param name="actorUserId">Identificativo necessario per individuare l'entità o il contesto di lavoro: actorUserId.</param>
    /// <param name="requestIp">Parametro necessario per l'operazione: requestIp.</param>
    /// <param name="userAgent">Parametro necessario per l'operazione: userAgent.</param>
    /// <returns>Restituisce in modo asincrono il risultato dell'operazione indicato dal tipo interno del Task quando la logica termina correttamente.</returns>
    /// <remarks>
    /// Effetti collaterali: scrive o aggiorna il database.
    /// </remarks>
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

    /// <summary>
    /// Esegue l''operazione ConsumeTokenAsync del servizio.
    /// </summary>
    /// <param name="rawToken">Token necessario per validare, rinnovare o revocare l'operazione richiesta.</param>
    /// <param name="purpose">Parametro necessario per l'operazione: purpose.</param>
    /// <returns>Restituisce in modo asincrono il risultato dell'operazione indicato dal tipo interno del Task quando la logica termina correttamente.</returns>
    /// <remarks>
    /// Effetti collaterali: scrive o aggiorna il database.
    /// </remarks>
    public async Task<bool> ConsumeTokenAsync(string rawToken, AccountActionTokenPurpose purpose)
    {
        var (valid, token) = await ValidateTokenAsync(rawToken, purpose);
        if (!valid || token is null) return false;

        token.UsedAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Esegue l''operazione di business RevokeActiveTokensAsync del servizio.
    /// </summary>
    /// <param name="userId">Identificativo necessario per individuare l'entità o il contesto di lavoro: userId.</param>
    /// <param name="purpose">Parametro necessario per l'operazione: purpose.</param>
    /// <returns>Completa l'operazione in modo asincrono senza restituire un valore, lasciando al chiamante la sola gestione dell'esito tramite eccezioni.</returns>
    /// <remarks>
    /// Effetti collaterali: scrive o aggiorna il database.
    /// </remarks>
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
