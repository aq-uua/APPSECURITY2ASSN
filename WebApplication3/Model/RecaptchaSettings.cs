namespace WebApplication3.Model;

public sealed class RecaptchaSettings
{
    public bool Enabled { get; set; }
    public string SiteKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public double ScoreThreshold { get; set; } = 0.5;
    public string VerifyUrl { get; set; } = "https://www.google.com/recaptcha/api/siteverify";
    public bool BypassInDevelopment { get; set; } = true;
}
