using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using WebApplication3.Model;
using WebApplication3.Services;

namespace WebApplication3.Pages;

[Authorize]
public class TwoFactorModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RecoverySettings _recoverySettings;
    private readonly IAuditLogger _auditLogger;
    private readonly IRecoveryCodeService _recoveryCodeService;

    public TwoFactorModel(
        UserManager<ApplicationUser> userManager,
        IOptions<RecoverySettings> recoverySettings,
        IAuditLogger auditLogger,
        IRecoveryCodeService recoveryCodeService)
    {
        _userManager = userManager;
        _recoverySettings = recoverySettings.Value;
        _auditLogger = auditLogger;
        _recoveryCodeService = recoveryCodeService;
    }

    public bool IsEnabled { get; private set; }
    public string[]? RecoveryCodes { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return RedirectToPage("/Login");
        }

        IsEnabled = await _userManager.GetTwoFactorEnabledAsync(user);
        return Page();
    }

    public async Task<IActionResult> OnPostEnableAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return RedirectToPage("/Login");
        }

        await _userManager.SetTwoFactorEnabledAsync(user, true);
        IsEnabled = true;

        await _auditLogger.LogAuthEventAsync(
            user.Id,
            AuditEventType.TwoFactorEnabled,
            "Two-factor enabled",
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString());

        return Page();
    }

    public async Task<IActionResult> OnPostDisableAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return RedirectToPage("/Login");
        }

        await _userManager.SetTwoFactorEnabledAsync(user, false);
        IsEnabled = false;

        await _auditLogger.LogAuthEventAsync(
            user.Id,
            AuditEventType.TwoFactorDisabled,
            "Two-factor disabled",
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString());

        return Page();
    }

    public async Task<IActionResult> OnPostGenerateRecoveryCodesAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return RedirectToPage("/Login");
        }

        IsEnabled = await _userManager.GetTwoFactorEnabledAsync(user);
        if (!IsEnabled)
        {
            return Page();
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var codes = await _recoveryCodeService.GenerateCodesAsync(user.Id, _recoverySettings.RecoveryCodeCount, ipAddress);
        RecoveryCodes = codes;

        await _auditLogger.LogAuthEventAsync(
            user.Id,
            AuditEventType.RecoveryCodesGenerated,
            "Recovery codes generated",
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString());

        return Page();
    }
}
