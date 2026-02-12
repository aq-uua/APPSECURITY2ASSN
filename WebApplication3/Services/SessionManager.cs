using Microsoft.EntityFrameworkCore;
using WebApplication3.Model;

namespace WebApplication3.Services;

public class SessionManager : ISessionManager
{
    private readonly AuthDbContext _context;
    private readonly ILogger<SessionManager> _logger;
    private readonly IConfiguration _configuration;

    public SessionManager(
        AuthDbContext context,
        ILogger<SessionManager> logger,
        IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<AuthSession> CreateSessionAsync(
        string userId,
        string? deviceInfo = null,
        string? ipAddress = null,
        DateTime? expiresAt = null)
    {
        var timeoutMinutes = _configuration.GetValue<int>("SessionSettings:TimeoutMinutes", 30);
        var actualExpiresAt = expiresAt ?? DateTime.UtcNow.AddMinutes(timeoutMinutes);

        var session = new AuthSession
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            SessionToken = Guid.NewGuid().ToString("N"),
            CreatedAt = DateTime.UtcNow,
            LastActiveAt = DateTime.UtcNow,
            ExpiresAt = actualExpiresAt,
            DeviceInfo = deviceInfo?.Substring(0, Math.Min(deviceInfo?.Length ?? 0, 500)),
            IpAddress = ipAddress?.Substring(0, Math.Min(ipAddress?.Length ?? 0, 45)),
            IsActive = true
        };

        _context.AuthSessions.Add(session);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Session created: {SessionId} for user: {UserId}, expires: {ExpiresAt}",
            session.Id,
            userId,
            actualExpiresAt);

        return session;
    }

    public async Task<bool> UpdateLastActivityAsync(Guid sessionId)
    {
        try
        {
            var session = await _context.AuthSessions
                .FirstOrDefaultAsync(s => s.Id == sessionId && s.IsActive);

            if (session == null)
            {
                return false;
            }

            if (session.ExpiresAt < DateTime.UtcNow)
            {
                session.IsActive = false;
                await _context.SaveChangesAsync();
                return false;
            }

            session.LastActiveAt = DateTime.UtcNow;
            
            var slidingExpiration = _configuration.GetValue<bool>("SessionSettings:SlidingExpiration", true);
            if (slidingExpiration)
            {
                var timeoutMinutes = _configuration.GetValue<int>("SessionSettings:TimeoutMinutes", 30);
                session.ExpiresAt = DateTime.UtcNow.AddMinutes(timeoutMinutes);
            }

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update session activity: {SessionId}", sessionId);
            return false;
        }
    }

    public async Task<bool> TerminateSessionAsync(Guid sessionId)
    {
        try
        {
            var session = await _context.AuthSessions
                .FirstOrDefaultAsync(s => s.Id == sessionId);

            if (session == null)
            {
                return false;
            }

            session.IsActive = false;
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Session terminated: {SessionId} for user: {UserId}",
                sessionId,
                session.UserId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to terminate session: {SessionId}", sessionId);
            return false;
        }
    }

    public async Task<int> TerminateUserSessionsAsync(string userId, Guid? exceptSessionId = null)
    {
        try
        {
            var query = _context.AuthSessions
                .Where(s => s.UserId == userId && s.IsActive);

            if (exceptSessionId.HasValue)
            {
                query = query.Where(s => s.Id != exceptSessionId.Value);
            }

            var sessions = await query.ToListAsync();

            foreach (var session in sessions)
            {
                session.IsActive = false;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Terminated {Count} sessions for user: {UserId}",
                sessions.Count,
                userId);

            return sessions.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to terminate user sessions: {UserId}", userId);
            return 0;
        }
    }

    public async Task<int> CleanupExpiredSessionsAsync()
    {
        try
        {
            var expiredSessions = await _context.AuthSessions
                .Where(s => s.IsActive && s.ExpiresAt < DateTime.UtcNow)
                .ToListAsync();

            foreach (var session in expiredSessions)
            {
                session.IsActive = false;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Cleaned up {Count} expired sessions",
                expiredSessions.Count);

            return expiredSessions.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup expired sessions");
            return 0;
        }
    }

    public async Task<AuthSession?> GetSessionAsync(Guid sessionId)
    {
        return await _context.AuthSessions
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.IsActive);
    }
}