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
public class ExpiredHoldCleanupService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ExpiredHoldCleanupService> _logger;
    private readonly TimeSpan _cleanupInterval;

    /// <summary>
    /// Esegue l''operazione ExpiredHoldCleanupService del servizio.
    /// </summary>
    /// <param name="scopeFactory">Parametro necessario per l'operazione: scopeFactory.</param>
    /// <param name="logger">Parametro necessario per l'operazione: logger.</param>
    /// <returns>Restituisce il risultato dell'operazione quando questa ha esito positivo; altrimenti il chiamante riceve un'eccezione o un risultato nullo/booleano secondo il contratto del metodo.</returns>
    /// <remarks>
    /// Effetti collaterali: scrive o aggiorna il database.
    /// </remarks>
    public ExpiredHoldCleanupService(IServiceScopeFactory scopeFactory, ILogger<ExpiredHoldCleanupService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;

        var cleanupIntervalMinutes = int.Parse(Environment.GetEnvironmentVariable("HOLD_CLEANUP_INTERVAL_MINUTES") ?? "5");
        _cleanupInterval = TimeSpan.FromMinutes(Math.Max(1, cleanupIntervalMinutes));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_cleanupInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupHoldsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante la pulizia degli hold scaduti");
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

    private async Task CleanupHoldsAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FilmDbContext>();
        var creditoService = scope.ServiceProvider.GetRequiredService<ICreditoService>();
        var now = DateTime.UtcNow;

        var toDelete = await db.ShowPostiStato
            .Where(sps => sps.Stato == ShowPostoState.Hold && sps.ScadeAtUtc <= now)
            .ToListAsync(cancellationToken);

        if (toDelete.Count > 0)
        {
            db.ShowPostiStato.RemoveRange(toDelete);
            await db.SaveChangesAsync(cancellationToken);
        }

        var expiredHostedOrders = await db.Ordini
            .Where(o => o.Stato == OrdineState.CheckoutInProgress && o.CheckoutExpiresAtUtc != null && o.CheckoutExpiresAtUtc <= now)
            .ToListAsync(cancellationToken);

        if (expiredHostedOrders.Count > 0)
        {
            var expiredOrderIds = expiredHostedOrders.Select(o => o.Id).ToList();
            var hostedSeats = await db.ShowPostiStato
                .Where(sps => sps.OrdineId != null && expiredOrderIds.Contains(sps.OrdineId.Value))
                .ToListAsync(cancellationToken);

            if (hostedSeats.Count > 0)
            {
                db.ShowPostiStato.RemoveRange(hostedSeats);
            }

            foreach (var ordine in expiredHostedOrders)
            {
                ordine.Stato = OrdineState.Expired;
                ordine.LastPaymentError ??= "Checkout scaduto.";

                if (ordine.CreditoRiservato > 0)
                {
                    await creditoService.ReleaseReservedOrderCreditAsync(
                        ordine.UserId,
                        ordine.Id,
                        $"Rilascio credito riservato ordine {ordine.CodiceOrdine} da cleanup automatico");
                    ordine.CreditoRiservato = 0m;
                }
            }

            await db.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation("Pulizia hold scaduti completata. Rimossi: {Count}", toDelete.Count);
    }
}
