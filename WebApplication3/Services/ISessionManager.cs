using WebApplication3.Model;

namespace WebApplication3.Services;

public interface ISessionManager
{
    Task<AuthSession> CreateSessionAsync(
        string userId,
        string? deviceInfo = null,
        string? ipAddress = null,
        DateTime? expiresAt = null);
    
    Task<bool> UpdateLastActivityAsync(Guid sessionId);
    
    Task<bool> TerminateSessionAsync(Guid sessionId);
    
    Task<int> TerminateUserSessionsAsync(string userId, Guid? exceptSessionId = null);
    
    Task<int> CleanupExpiredSessionsAsync();
    
    Task<AuthSession?> GetSessionAsync(Guid sessionId);
}