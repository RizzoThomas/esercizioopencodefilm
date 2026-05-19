using FilmAPI.Data;
using FilmAPI.Model;

namespace FilmAPI.Services;

/// <summary>
/// Fornisce il servizio  per le operazioni di dominio esposte da questo modulo.
/// </summary>
/// <remarks>
/// Usato dai controller o endpoint che gestiscono le funzioni di . Dipendenze iniettate nel costruttore: nessuna dichiarata esplicitamente.
/// </remarks>
public class UserSecurityAuditService : IUserSecurityAuditService
{
    private readonly FilmDbContext _context;

    /// <summary>
    /// Esegue l''operazione UserSecurityAuditService del servizio.
    /// </summary>
    /// <param name="context">Parametro necessario per l'operazione: context.</param>
    /// <returns>Restituisce il risultato dell'operazione quando questa ha esito positivo; altrimenti il chiamante riceve un'eccezione o un risultato nullo/booleano secondo il contratto del metodo.</returns>
    /// <remarks>
    /// Effetti collaterali: scrive o aggiorna il database.
    /// </remarks>
    public UserSecurityAuditService(FilmDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Esegue l''operazione LogAsync del servizio.
    /// </summary>
    /// <param name="userId">Identificativo necessario per individuare l'entità o il contesto di lavoro: userId.</param>
    /// <param name="actorUserId">Identificativo necessario per individuare l'entità o il contesto di lavoro: actorUserId.</param>
    /// <param name="eventType">Parametro necessario per l'operazione: eventType.</param>
    /// <param name="provider">Parametro necessario per l'operazione: provider.</param>
    /// <param name="ipAddress">Parametro necessario per l'operazione: ipAddress.</param>
    /// <param name="userAgent">Parametro necessario per l'operazione: userAgent.</param>
    /// <param name="metadataJson">Parametro necessario per l'operazione: metadataJson.</param>
    /// <returns>Completa l'operazione in modo asincrono senza restituire un valore, lasciando al chiamante la sola gestione dell'esito tramite eccezioni.</returns>
    /// <remarks>
    /// Effetti collaterali: scrive o aggiorna il database.
    /// </remarks>
    public async Task LogAsync(int? userId, int? actorUserId, string eventType, string? provider = null, string? ipAddress = null, string? userAgent = null, string? metadataJson = null)
    {
        var log = new UserSecurityAuditLog
        {
            UserId = userId,
            ActorUserId = actorUserId,
            EventType = eventType,
            Provider = provider,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            MetadataJson = metadataJson,
            CreatedAtUtc = DateTime.UtcNow
        };

        _context.UserSecurityAuditLogs.Add(log);
        await _context.SaveChangesAsync();
    }
}
