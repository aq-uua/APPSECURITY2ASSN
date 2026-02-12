namespace WebApplication3.ViewModels;

public class PasswordResetEmailModel
{
    public string ResetLink { get; set; } = string.Empty;
    public int ExpiresMinutes { get; set; }
}
