namespace WebApplication3.Model;

public sealed class SessionSettings
{
    public int TimeoutMinutes { get; set; } = 30;
    public bool SlidingExpiration { get; set; } = true;
    public int CleanupIntervalMinutes { get; set; } = 30;
}
