using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.WebUtilities;
using WebApplication3.Model;
using WebApplication3.Services;
using WebApplication3.ViewModels;

namespace WebApplication3.Pages;

public class ForgotPasswordModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailSender _emailSender;
    private readonly IRazorViewToStringRenderer _renderer;
    private readonly IRecaptchaVerifier _recaptchaVerifier;
    private readonly RecaptchaSettings _recaptchaSettings;
    private readonly RecoverySettings _recoverySettings;
    private readonly IAuditLogger _auditLogger;

    public ForgotPasswordModel(
        UserManager<ApplicationUser> userManager,
        IEmailSender emailSender,
        IRazorViewToStringRenderer renderer,
        IRecaptchaVerifier recaptchaVerifier,
        IOptions<RecaptchaSettings> recaptchaSettings,
        IOptions<RecoverySettings> recoverySettings,
        IAuditLogger auditLogger)
    {
        _userManager = userManager;
        _emailSender = emailSender;
        _renderer = renderer;
        _recaptchaVerifier = recaptchaVerifier;
        _recaptchaSettings = recaptchaSettings.Value;
        _recoverySettings = recoverySettings.Value;
        _auditLogger = auditLogger;
    }

    [BindProperty]
    public ForgotPassword Input { get; set; } = new();

    public bool RecaptchaEnabled => _recaptchaSettings.Enabled;
    public string RecaptchaSiteKey => _recaptchaSettings.SiteKey;

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = Request.Headers.UserAgent.ToString();

        var recaptchaValid = await _recaptchaVerifier.VerifyAsync(Input.RecaptchaToken, ipAddress);
        if (!recaptchaValid)
        {
            await _auditLogger.LogAuthEventAsync(
                null,
                AuditEventType.RecaptchaFailed,
                "Forgot password recaptcha failed",
                ipAddress,
                userAgent);
            ModelState.AddModelError(string.Empty, "Security verification failed. Please try again.");
            return Page();
        }

        var email = Input.Email.Trim();
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
        {
            await _auditLogger.LogAuthEventAsync(
                null,
                AuditEventType.PasswordResetRequested,
                "Password reset requested for unknown or unconfirmed email",
                ipAddress,
                userAgent);
            return RedirectToPage("/ForgotPasswordConfirmation");
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        var callbackUrl = Url.Page(
            "/ResetPassword",
            null,
            new { email = user.Email, token = encodedToken },
            Request.Scheme);

        var model = new PasswordResetEmailModel
        {
            ResetLink = callbackUrl ?? string.Empty,
            ExpiresMinutes = _recoverySettings.PasswordResetTokenMinutes
        };

        var html = await _renderer.RenderViewToStringAsync("~/Services/EmailTemplates/PasswordResetEmail.cshtml", model);
        await _emailSender.SendEmailAsync(user.Email!, "Reset your Farm Fresh Market password", html);

        await _auditLogger.LogAuthEventAsync(
            user.Id,
            AuditEventType.PasswordResetRequested,
            "Password reset email sent",
            ipAddress,
            userAgent);

        return RedirectToPage("/ForgotPasswordConfirmation");
    }
}
