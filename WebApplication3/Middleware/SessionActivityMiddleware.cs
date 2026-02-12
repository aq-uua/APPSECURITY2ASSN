using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using WebApplication3.Model;
using WebApplication3.Services;

namespace WebApplication3.Middleware;

public sealed class SessionActivityMiddleware
{
    private const string SessionIdKey = "AuthSessionId";
    private readonly RequestDelegate _next;
    private readonly ILogger<SessionActivityMiddleware> _logger;

    public SessionActivityMiddleware(RequestDelegate next, ILogger<SessionActivityMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ISessionManager sessionManager, IAuditLogger auditLogger)
    {
        if (context.User?.Identity?.IsAuthenticated == true)
        {
            var sessionIdValue = context.Session.GetString(SessionIdKey);
            if (Guid.TryParse(sessionIdValue, out var sessionId))
            {
                var session = await sessionManager.GetSessionAsync(sessionId);
                if (session is null)
                {
                    await HandleExpiredSessionAsync(context, auditLogger, "Session not found");
                    return;
                }

                var updated = await sessionManager.UpdateLastActivityAsync(sessionId);
                if (!updated)
                {
                    await HandleExpiredSessionAsync(context, auditLogger, "Session expired");
                    return;
                }
            }
        }

        await _next(context);
    }

    private async Task HandleExpiredSessionAsync(HttpContext context, IAuditLogger auditLogger, string reason)
    {
        var userId = context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        var ipAddress = context.Connection.RemoteIpAddress?.ToString();
        var userAgent = context.Request.Headers.UserAgent.ToString();

        await auditLogger.LogAuthEventAsync(userId, AuditEventType.SessionExpired, reason, ipAddress, userAgent);
        await context.SignOutAsync(IdentityConstants.ApplicationScheme);
        context.Session.Remove(SessionIdKey);

        _logger.LogInformation("Session terminated: {Reason}", reason);
        context.Response.Redirect("/Login");
    }
}
