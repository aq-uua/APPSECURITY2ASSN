using Microsoft.Extensions.Options;
using WebApplication3.Model;

namespace WebApplication3.Services;

public sealed class SessionCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SessionCleanupService> _logger;
    private readonly SessionSettings _settings;

    public SessionCleanupService(
        IServiceProvider serviceProvider,
        IOptions<SessionSettings> settings,
        ILogger<SessionCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _settings = settings.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalMinutes = Math.Max(_settings.CleanupIntervalMinutes, 5);
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(intervalMinutes));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var sessionManager = scope.ServiceProvider.GetRequiredService<ISessionManager>();
                var auditLogger = scope.ServiceProvider.GetRequiredService<IAuditLogger>();

                var cleaned = await sessionManager.CleanupExpiredSessionsAsync();
                if (cleaned > 0)
                {
                    await auditLogger.LogEventAsync(null, AuditEventType.SessionExpired, $"Cleanup terminated {cleaned} sessions");
                    _logger.LogInformation("Session cleanup terminated {Count} sessions", cleaned);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Session cleanup failed");
            }
        }
    }
}
