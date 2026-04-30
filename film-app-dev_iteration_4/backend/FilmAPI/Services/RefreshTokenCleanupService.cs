using FilmAPI.Data;
using Microsoft.EntityFrameworkCore;

namespace FilmAPI.Services;

public class RefreshTokenCleanupService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RefreshTokenCleanupService> _logger;
    private readonly TimeSpan _cleanupInterval;

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
