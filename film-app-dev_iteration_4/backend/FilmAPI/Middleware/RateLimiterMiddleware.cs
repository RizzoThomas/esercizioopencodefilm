using System.Collections.Concurrent;

namespace FilmAPI.Middleware;

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

    public RateLimiterMiddleware(RequestDelegate next)
    {
        _next = next;
    }

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

    private class RateLimitEntry
    {
        public DateTime WindowStart { get; set; }
        public int Count { get; set; }
    }
}
