namespace WebApplication3.Model;

public sealed class RecoverySettings
{
    public int PasswordResetTokenMinutes { get; set; } = 30;
    public int TwoFactorTokenMinutes { get; set; } = 10;
    public int RecoveryCodeCount { get; set; } = 8;
}
