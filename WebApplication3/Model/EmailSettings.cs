namespace WebApplication3.Model;

public sealed class EmailSettings
{
    public string SmtpServer { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 587;
    public string SmtpUsername { get; set; } = string.Empty;
    public string SmtpPassword { get; set; } = string.Empty;
    public string FromEmail { get; set; } = "noreply@freshfarmmarket.com";
    public string FromName { get; set; } = "Farm Fresh Market";
    public bool UseSsl { get; set; } = true;
    public string Security { get; set; } = "StartTls";
    public int TimeoutSeconds { get; set; } = 30;
}
