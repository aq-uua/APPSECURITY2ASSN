namespace WebApplication3.ViewModels;

public class TwoFactorEmailModel
{
    public string Code { get; set; } = string.Empty;
    public int ExpiresMinutes { get; set; }
}
