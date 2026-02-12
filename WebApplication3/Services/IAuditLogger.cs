using WebApplication3.Model;

namespace WebApplication3.Services;

public interface IAuditLogger
{
    Task LogAuthEventAsync(
        string? userId,
        AuditEventType eventType,
        string? details = null,
        string? ipAddress = null,
        string? userAgent = null);
    
    Task LogEventAsync(
        string? userId,
        AuditEventType eventType,
        string? details = null,
        string? ipAddress = null,
        string? userAgent = null);
}
