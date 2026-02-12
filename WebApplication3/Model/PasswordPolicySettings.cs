namespace WebApplication3.Model;

public sealed class PasswordPolicySettings
{
    public int PasswordHistoryCount { get; set; } = 2;
    public int MinPasswordAgeDays { get; set; } = 1;
    public int MaxPasswordAgeDays { get; set; } = 90;
}
