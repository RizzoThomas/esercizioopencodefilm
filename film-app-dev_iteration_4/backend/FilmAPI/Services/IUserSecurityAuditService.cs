namespace FilmAPI.Services;

public interface IUserSecurityAuditService
{
    Task LogAsync(int? userId, int? actorUserId, string eventType, string? provider = null, string? ipAddress = null, string? userAgent = null, string? metadataJson = null);
}
