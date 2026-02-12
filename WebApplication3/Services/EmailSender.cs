using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using WebApplication3.Model;

namespace WebApplication3.Services;

public class EmailSender : IEmailSender
{
    private readonly EmailSettings _settings;
    private readonly ILogger<EmailSender> _logger;

    public EmailSender(IOptions<EmailSettings> options, ILogger<EmailSender> logger)
    {
        _settings = options.Value;
        _logger = logger;
    }

    private static string MaskEmail(string email)
    {
        if (string.IsNullOrEmpty(email) || !email.Contains('@'))
            return email ?? "null";
        
        var parts = email.Split('@');
        var local = parts[0];
        var domain = parts[1];
        
        if (local.Length <= 1)
            return $"{local}***@{domain}";
        
        return $"{local[0]}***@{domain}";
    }

    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        try
        {
            using var mailMessage = new MailMessage
            {
                From = new MailAddress(_settings.FromEmail, _settings.FromName),
                Subject = subject,
                Body = htmlMessage,
                IsBodyHtml = true
            };

            mailMessage.To.Add(email);

            using var client = new SmtpClient(_settings.SmtpServer, _settings.SmtpPort)
            {
                Timeout = Math.Max(_settings.TimeoutSeconds, 5) * 1000,
                EnableSsl = _settings.Security switch
                {
                    "None" => false,
                    "StartTls" => true,
                    "StartTlsWhenAvailable" => true,
                    "SslOnConnect" => true,
                    _ => _settings.UseSsl
                },
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false
            };

            if (!string.IsNullOrWhiteSpace(_settings.SmtpUsername))
            {
                client.Credentials = new NetworkCredential(_settings.SmtpUsername, _settings.SmtpPassword);
            }

            await client.SendMailAsync(mailMessage);

            _logger.LogInformation("Email sent to {Email} subject={Subject}", MaskEmail(email), subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email}", MaskEmail(email));
            throw;
        }
    }
}
