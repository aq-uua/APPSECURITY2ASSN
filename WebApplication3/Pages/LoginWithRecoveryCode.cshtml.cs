using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using WebApplication3.Model;
using WebApplication3.Services;
using WebApplication3.ViewModels;

namespace WebApplication3.Pages;

public class LoginWithRecoveryCodeModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IRecoveryCodeService _recoveryCodeService;
    private readonly PasswordPolicySettings _passwordPolicy;
    private readonly IAuditLogger _auditLogger;
    private readonly ILogger<LoginWithRecoveryCodeModel> _logger;

    public LoginWithRecoveryCodeModel(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        IRecoveryCodeService recoveryCodeService,
        IOptions<PasswordPolicySettings> passwordPolicy,
        IAuditLogger auditLogger,
        ILogger<LoginWithRecoveryCodeModel> logger)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _recoveryCodeService = recoveryCodeService;
        _passwordPolicy = passwordPolicy.Value;
        _auditLogger = auditLogger;
        _logger = logger;
    }

    [BindProperty]
    public RecoveryCodeLogin Input { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    [TempData]
    public string? StatusMessage { get; set; }

    public void OnGet(bool rememberMe, bool rememberMachine, string? returnUrl = null)
    {
        ReturnUrl = returnUrl ?? Url.Content("~/");
        Input.RememberMe = rememberMe;
        Input.RememberMachine = rememberMachine;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = Request.Headers.UserAgent.ToString();

        // Get the user from the two-factor authentication session
        var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
        if (user == null)
        {
            return RedirectToPage("/Login");
        }

        // Clean the recovery code (remove spaces and hyphens)
        var recoveryCode = Input.RecoveryCode
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .ToUpperInvariant();

        // Validate and redeem the recovery code using our custom service
        var isValid = await _recoveryCodeService.ValidateAndRedeemAsync(user.Id, recoveryCode, ipAddress);

        if (isValid)
        {
            _logger.LogInformation("User {Email} signed in with recovery code", user.Email);
            await _auditLogger.LogAuthEventAsync(
                user.Id,
                AuditEventType.RecoveryCodeUsed,
                "Recovery code login successful",
                ipAddress,
                userAgent);

            // Sign in the user manually
            await _signInManager.SignInAsync(user, Input.RememberMe);

            // Handle RememberMachine (two-factor remember me cookie)
            if (Input.RememberMachine)
            {
                await _signInManager.RememberTwoFactorClientAsync(user);
            }

            // Hard expiry check — force password change if expired
            if (_passwordPolicy.IsPasswordExpired(user.PasswordLastChangedAt))
            {
                await _auditLogger.LogAuthEventAsync(
                    user.Id,
                    AuditEventType.PasswordExpired,
                    "Password expired after recovery code login",
                    ipAddress,
                    userAgent);

                TempData["StatusMessage"] = "Your password has expired. Please update it to continue.";
                return RedirectToPage("/ChangePassword");
            }

            return LocalRedirect(ReturnUrl ?? Url.Content("~/"));
        }

        // Recovery code failed - log the attempt
        await _auditLogger.LogAuthEventAsync(
            user.Id,
            AuditEventType.TwoFactorChallengeFailed,
            "Recovery code login failed",
            ipAddress,
            userAgent);

        ModelState.AddModelError(string.Empty, "Invalid recovery code. Please try again or use a different code.");
        return Page();
    }
}
