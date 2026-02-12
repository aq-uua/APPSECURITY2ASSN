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
    private readonly IAuditLogger _auditLogger;

    public LoginWith2faModel(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        IEmailSender emailSender,
        IRazorViewToStringRenderer renderer,
        IOptions<RecoverySettings> recoverySettings,
        IAuditLogger auditLogger)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _emailSender = emailSender;
        _renderer = renderer;
        _recoverySettings = recoverySettings.Value;
        _auditLogger = auditLogger;
    }

    [BindProperty]
    public LoginWith2fa Input { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public async Task<IActionResult> OnGetAsync(bool rememberMe, string? returnUrl = null)
    {
        ReturnUrl = returnUrl ?? Url.Content("~/");
        Input.RememberMe = rememberMe;

        var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
        if (user == null)
        {
            return RedirectToPage("/Login");
        }

        var code = await _userManager.GenerateTwoFactorTokenAsync(user, TokenOptions.DefaultEmailProvider);
        var model = new TwoFactorEmailModel
        {
            Code = code,
            ExpiresMinutes = _recoverySettings.TwoFactorTokenMinutes
        };

        var html = await _renderer.RenderViewToStringAsync("~/Services/EmailTemplates/TwoFactorEmail.cshtml", model);
        await _emailSender.SendEmailAsync(user.Email!, "Your Farm Fresh Market verification code", html);

        await _auditLogger.LogAuthEventAsync(
            user.Id,
            AuditEventType.TwoFactorChallengeSent,
            "Two-factor email code sent",
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString());

        return Page();
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
        return Page();
    }
}
