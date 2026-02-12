using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using WebApplication3.Model;

namespace WebApplication3.Services;

public class AuditLogger : IAuditLogger
{
    private readonly AuthDbContext _context;
    private readonly ILogger<AuditLogger> _logger;

    public AuditLogger(AuthDbContext context, ILogger<AuditLogger> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Sanitizes input to prevent log injection attacks by removing control characters.
    /// </summary>
    private static string SanitizeForLogging(string? input)
    {
        if (string.IsNullOrEmpty(input))
            return input ?? "null";

        // Remove control characters, newlines, carriage returns, null bytes, and tabs
        // that could be used for log injection attacks
        return input
            .Replace("\n", "")
            .Replace("\r", "")
            .Replace("\0", "")
            .Replace("\t", " ");
    }

    public async Task LogAuthEventAsync(
        string? userId,
        AuditEventType eventType,
        string? details = null,
        string? ipAddress = null,
        string? userAgent = null)
    {
        await LogEventAsync(userId, eventType, details, ipAddress, userAgent);
    }

    public async Task LogEventAsync(
        string? userId,
        AuditEventType eventType,
        string? details = null,
        string? ipAddress = null,
        string? userAgent = null)
    {
        try
        {
            var eventData = new Dictionary<string, object>();
            
            if (!string.IsNullOrEmpty(details))
            {
                eventData["details"] = details;
            }
            
            if (!string.IsNullOrEmpty(userAgent))
            {
                eventData["userAgent"] = userAgent;
            }

            var auditLog = new AuditLog
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                EventType = eventType.ToString(),
                EventData = JsonSerializer.Serialize(eventData),
                IpAddress = ipAddress?.Substring(0, Math.Min(ipAddress?.Length ?? 0, 45)),
                UserAgent = userAgent?.Substring(0, Math.Min(userAgent?.Length ?? 0, 500)),
                CreatedAt = DateTime.UtcNow
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Audit event logged: {EventType} for user {UserId}",
                SanitizeForLogging(eventType.ToString()),
                SanitizeForLogging(userId) ?? "anonymous");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log audit event: {EventType}", SanitizeForLogging(eventType.ToString()));
        }
    }
}
