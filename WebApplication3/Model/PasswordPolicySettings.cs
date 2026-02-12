namespace WebApplication3.Model;

public sealed class PasswordPolicySettings
{
    public int PasswordHistoryCount { get; set; } = 2;
    public int MinPasswordAgeMinutes { get; set; } = 1;
    public int PasswordWarningAgeMinutes { get; set; } = 5;
    public int MaxPasswordAgeMinutes { get; set; } = 10;

    public bool IsPasswordExpired(DateTime? passwordLastChangedAt)
        => passwordLastChangedAt.HasValue
           && DateTime.UtcNow >= passwordLastChangedAt.Value.AddMinutes(MaxPasswordAgeMinutes);

    public bool IsPasswordNearExpiry(DateTime? passwordLastChangedAt)
        => passwordLastChangedAt.HasValue
           && DateTime.UtcNow >= passwordLastChangedAt.Value.AddMinutes(PasswordWarningAgeMinutes);
}
