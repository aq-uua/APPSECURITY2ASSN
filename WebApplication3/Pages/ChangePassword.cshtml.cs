using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WebApplication3.Model;
using WebApplication3.Services;
using WebApplication3.ViewModels;

namespace WebApplication3.Pages;

[Authorize]
public class ChangePasswordModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly AuthDbContext _context;
    private readonly PasswordPolicySettings _settings;
    private readonly IAuditLogger _auditLogger;

    public ChangePasswordModel(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        AuthDbContext context,
        IOptions<PasswordPolicySettings> settings,
        IAuditLogger auditLogger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _context = context;
        _settings = settings.Value;
        _auditLogger = auditLogger;
    }

    [BindProperty]
    public ChangePassword Input { get; set; } = new();

    [TempData]
    public string? StatusMessage { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return RedirectToPage("/Login");
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = Request.Headers.UserAgent.ToString();

        if (user.PasswordLastChangedAt.HasValue && _settings.MinPasswordAgeMinutes > 0)
        {
            var minChangeDate = user.PasswordLastChangedAt.Value.AddMinutes(_settings.MinPasswordAgeMinutes);
            if (DateTime.UtcNow < minChangeDate)
            {
                ModelState.AddModelError("", "Password created is too recent to be changed, please wait before changing again.");
                await _auditLogger.LogAuthEventAsync(user.Id, AuditEventType.PasswordChangeFailed, "Minimum password age not met", ipAddress, userAgent);
                return Page();
            }
        }

        var historyCount = Math.Max(_settings.PasswordHistoryCount, 0);
        if (historyCount > 0)
        {
            var recentHashes = await _context.PasswordHistories
                .Where(h => h.UserId == user.Id)
                .OrderByDescending(h => h.CreatedAt)
                .Take(historyCount)
                .Select(h => h.PasswordHash)
                .ToListAsync();

            foreach (var hash in recentHashes)
            {
                var verify = _userManager.PasswordHasher.VerifyHashedPassword(user, hash, Input.NewPassword);
                if (verify == PasswordVerificationResult.Success)
                {
                    ModelState.AddModelError("", "You cannot reuse a recent password.");
                    await _auditLogger.LogAuthEventAsync(user.Id, AuditEventType.PasswordChangeFailed, "Password reuse blocked", ipAddress, userAgent);
                    return Page();
                }
            }
        }

        var result = await _userManager.ChangePasswordAsync(user, Input.CurrentPassword, Input.NewPassword);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            await _auditLogger.LogAuthEventAsync(user.Id, AuditEventType.PasswordChangeFailed, "Change password failed", ipAddress, userAgent);
            return Page();
        }

        user.PasswordLastChangedAt = DateTime.UtcNow;

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            foreach (var error in updateResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            await _auditLogger.LogAuthEventAsync(user.Id, AuditEventType.PasswordChangeFailed, "User update failed", ipAddress, userAgent);
            return Page();
        }

        _context.PasswordHistories.Add(new PasswordHistory
        {
            UserId = user.Id,
            PasswordHash = user.PasswordHash ?? string.Empty,
            CreatedAt = DateTime.UtcNow
        });

        if (historyCount > 0)
        {
            var removeCount = await _context.PasswordHistories
                .Where(h => h.UserId == user.Id)
                .OrderByDescending(h => h.CreatedAt)
                .Skip(historyCount)
                .ToListAsync();

            if (removeCount.Count > 0)
            {
                _context.PasswordHistories.RemoveRange(removeCount);
            }
        }

        await _context.SaveChangesAsync();
        await _signInManager.RefreshSignInAsync(user);
        await _auditLogger.LogAuthEventAsync(user.Id, AuditEventType.PasswordChanged, "Password changed", ipAddress, userAgent);

        StatusMessage = "Your password has been updated.";
        return RedirectToPage("/Index");
    }
}
