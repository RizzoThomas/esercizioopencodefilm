using FilmAPI.Data;
using Microsoft.EntityFrameworkCore;

namespace FilmAPI.Services;

/// <summary>
/// Fornisce il servizio  per le operazioni di dominio esposte da questo modulo.
/// </summary>
/// <remarks>
/// Usato dai controller o endpoint che gestiscono le funzioni di . Dipendenze iniettate nel costruttore: nessuna dichiarata esplicitamente.
/// </remarks>
public class RefreshTokenCleanupService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RefreshTokenCleanupService> _logger;
    private readonly TimeSpan _cleanupInterval;

    /// <summary>
    /// Esegue l''operazione di business RefreshTokenCleanupService del servizio.
    /// </summary>
    /// <param name="scopeFactory">Parametro necessario per l'operazione: scopeFactory.</param>
    /// <param name="logger">Parametro necessario per l'operazione: logger.</param>
    /// <returns>Restituisce il risultato dell'operazione quando questa ha esito positivo; altrimenti il chiamante riceve un'eccezione o un risultato nullo/booleano secondo il contratto del metodo.</returns>
    /// <remarks>
    /// Effetti collaterali: scrive o aggiorna il database.
    /// </remarks>
    public RefreshTokenCleanupService(IServiceScopeFactory scopeFactory, ILogger<RefreshTokenCleanupService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;

        var cleanupIntervalMinutes = int.Parse(Environment.GetEnvironmentVariable("REFRESH_TOKEN_CLEANUP_INTERVAL_MINUTES") ?? "30");
        _cleanupInterval = TimeSpan.FromMinutes(Math.Max(1, cleanupIntervalMinutes));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_cleanupInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupTokensAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante la pulizia dei refresh token");
            }

            try
            {
                await timer.WaitForNextTickAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task CleanupTokensAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FilmDbContext>();
        var now = DateTime.UtcNow;

        var toDelete = await db.RefreshTokens
            .Where(rt => rt.RevokedAt != null || rt.ExpiresAt <= now)
            .ToListAsync(cancellationToken);

        if (toDelete.Count == 0) return;

        db.RefreshTokens.RemoveRange(toDelete);
        await db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Pulizia refresh token completata. Rimossi: {Count}", toDelete.Count);
    }
}
