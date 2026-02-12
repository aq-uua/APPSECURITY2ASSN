using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using WebApplication3.Model;
using WebApplication3.Services;
using WebApplication3.ViewModels;

namespace WebApplication3.Pages;

public class LoginWith2faModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailSender _emailSender;
    private readonly IRazorViewToStringRenderer _renderer;
    private readonly RecoverySettings _recoverySettings;
    private readonly PasswordPolicySettings _passwordPolicy;
    private readonly IAuditLogger _auditLogger;

    public LoginWith2faModel(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        IEmailSender emailSender,
        IRazorViewToStringRenderer renderer,
        IOptions<RecoverySettings> recoverySettings,
        IOptions<PasswordPolicySettings> passwordPolicy,
        IAuditLogger auditLogger)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _emailSender = emailSender;
        _renderer = renderer;
        _recoverySettings = recoverySettings.Value;
        _passwordPolicy = passwordPolicy.Value;
        _auditLogger = auditLogger;
    }

    private const string ResendCooldownKey = "2fa_resend_cooldown";
    private const int ResendCooldownSeconds = 60;

    [BindProperty]
    public LoginWith2fa Input { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    [TempData]
    public string? StatusMessage { get; set; }

    public bool CanResend { get; private set; } = true;
    public int SecondsRemaining { get; private set; } = 0;

    public async Task<IActionResult> OnGetAsync(bool rememberMe, string? returnUrl = null)
    {
        ReturnUrl = returnUrl ?? Url.Content("~/");
        Input.RememberMe = rememberMe;

        var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
        if (user == null)
        {
            return RedirectToPage("/Login");
        }

        // Check cooldown
        CheckCooldown();

        // Send initial code
        await SendTwoFactorCodeAsync(user, "Two-factor email code sent");

        return Page();
    }

    private void CheckCooldown()
    {
        var lastSent = HttpContext.Session.GetString(ResendCooldownKey);
        if (!string.IsNullOrEmpty(lastSent) && long.TryParse(lastSent, out var ticks))
        {
            var lastSentTime = new DateTime(ticks);
            var elapsed = DateTime.UtcNow - lastSentTime;
            if (elapsed.TotalSeconds < ResendCooldownSeconds)
            {
                CanResend = false;
                SecondsRemaining = ResendCooldownSeconds - (int)elapsed.TotalSeconds;
            }
        }
    }

    private async Task SendTwoFactorCodeAsync(ApplicationUser user, string auditMessage)
    {
        var code = await _userManager.GenerateTwoFactorTokenAsync(user, TokenOptions.DefaultEmailProvider);
        var model = new TwoFactorEmailModel
        {
            Code = code,
            ExpiresMinutes = _recoverySettings.TwoFactorTokenMinutes
        };

        var html = await _renderer.RenderViewToStringAsync("~/Services/EmailTemplates/TwoFactorEmail.cshtml", model);
        await _emailSender.SendEmailAsync(user.Email!, "Your Farm Fresh Market verification code", html);

        // Set cooldown
        HttpContext.Session.SetString(ResendCooldownKey, DateTime.UtcNow.Ticks.ToString());

        await _auditLogger.LogAuthEventAsync(
            user.Id,
            AuditEventType.TwoFactorChallengeSent,
            auditMessage,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString());
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
        if (user == null)
        {
            return RedirectToPage("/Login");
        }

        var code = Input.Code.Replace(" ", string.Empty, StringComparison.Ordinal).Replace("-", string.Empty, StringComparison.Ordinal);
        var result = await _signInManager.TwoFactorSignInAsync(
            TokenOptions.DefaultEmailProvider,
            code,
            Input.RememberMe,
            Input.RememberMachine);

        if (result.Succeeded)
        {
            await _auditLogger.LogAuthEventAsync(
                user.Id,
                AuditEventType.TwoFactorChallengeSucceeded,
                "Two-factor verification succeeded",
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers.UserAgent.ToString());

            // Hard expiry check — force password change if expired
            if (_passwordPolicy.IsPasswordExpired(user.PasswordLastChangedAt))
            {
                await _auditLogger.LogAuthEventAsync(
                    user.Id,
                    AuditEventType.PasswordExpired,
                    "Password expired after 2FA login",
                    HttpContext.Connection.RemoteIpAddress?.ToString(),
                    Request.Headers.UserAgent.ToString());

                TempData["StatusMessage"] = "Your password has expired. Please update it to continue.";
                return RedirectToPage("/ChangePassword");
            }

            return LocalRedirect(ReturnUrl ?? Url.Content("~/"));
        }

        if (result.IsLockedOut)
        {
            await _auditLogger.LogAuthEventAsync(
                user.Id,
                AuditEventType.TwoFactorChallengeFailed,
                "Two-factor verification locked out",
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers.UserAgent.ToString());
            return RedirectToPage("/Lockout");
        }

        await _auditLogger.LogAuthEventAsync(
            user.Id,
            AuditEventType.TwoFactorChallengeFailed,
            "Two-factor verification failed",
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString());

        ModelState.AddModelError(string.Empty, "Invalid verification code.");
        CheckCooldown();
        return Page();
    }

    public async Task<IActionResult> OnPostResendCodeAsync()
    {
        ReturnUrl = ReturnUrl ?? Url.Content("~/");
        
        var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
        if (user == null)
        {
            return RedirectToPage("/Login");
        }

        // Check cooldown
        CheckCooldown();
        
        if (!CanResend)
        {
            StatusMessage = "Please wait before requesting a new code.";
            return Page();
        }

        await SendTwoFactorCodeAsync(user, "Two-factor email code resent");
        StatusMessage = "A new verification code has been sent to your email.";
        
        // Re-check cooldown after sending
        CheckCooldown();
        
        return Page();
    }
}
