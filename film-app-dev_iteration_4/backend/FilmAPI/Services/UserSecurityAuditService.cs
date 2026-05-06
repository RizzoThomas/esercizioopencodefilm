using FilmAPI.Data;
using FilmAPI.Model;

namespace FilmAPI.Services;

public class UserSecurityAuditService : IUserSecurityAuditService
{
    private readonly FilmDbContext _context;

    public UserSecurityAuditService(FilmDbContext context)
    {
        _context = context;
    }

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
