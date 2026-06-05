using System.Collections.Concurrent;

namespace FilmAPI.Middleware;

/// <summary>
/// Middleware che limita le richieste sui percorsi sensibili.
/// </summary>
public class RateLimiterMiddleware
{
    private readonly RequestDelegate _next;
    private static readonly ConcurrentDictionary<string, RateLimitEntry> _entries = new();
    private const int MaxRequests = 5;
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(1);

    private static readonly HashSet<string> LimitedPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/auth/login",
        "/auth/forgot-password"
    };

    /// <summary>
    /// Inizializza il middleware con il componente successivo.
    /// </summary>
    /// <param name="next">Delegate della pipeline successiva.</param>
    public RateLimiterMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    /// <summary>
    /// Applica il limite richieste e prosegue la pipeline se consentito.
    /// </summary>
    /// <param name="context">Contesto HTTP corrente.</param>
    /// <returns>Attività asincrona del middleware.</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value;
        if (path is not null && LimitedPaths.Contains(path))
        {
            var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var key = $"{ip}:{path}";
            var now = DateTime.UtcNow;

            var entry = _entries.GetOrAdd(key, _ => new RateLimitEntry { WindowStart = now, Count = 0 });

            lock (entry)
            {
                if (now - entry.WindowStart > Window)
                {
                    entry.WindowStart = now;
                    entry.Count = 0;
                }

                entry.Count++;

                if (entry.Count > MaxRequests)
                {
                    context.Response.StatusCode = 429;
                    context.Response.Headers["Retry-After"] = ((int)(Window - (now - entry.WindowStart)).TotalSeconds + 1).ToString();
                    return;
                }
            }
        }

        await _next(context);
    }

    /// <summary>
    /// Memorizza finestra temporale e conteggio richieste per chiave IP/percorso.
    /// </summary>
    private class RateLimitEntry
    {
        /// <summary>
        /// Inizio della finestra corrente.
        /// </summary>
        public DateTime WindowStart { get; set; }
        /// <summary>
        /// Conteggio richieste nella finestra corrente.
        /// </summary>
        public int Count { get; set; }
    }
}
