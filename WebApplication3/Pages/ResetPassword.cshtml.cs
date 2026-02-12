using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.WebUtilities;
using WebApplication3.Model;
using WebApplication3.Services;
using WebApplication3.ViewModels;

namespace WebApplication3.Pages;

public class ResetPasswordModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AuthDbContext _context;
    private readonly PasswordPolicySettings _passwordPolicy;
    private readonly RecoverySettings _recoverySettings;
    private readonly IAuditLogger _auditLogger;

    public ResetPasswordModel(
        UserManager<ApplicationUser> userManager,
        AuthDbContext context,
        IOptions<PasswordPolicySettings> passwordPolicy,
        IOptions<RecoverySettings> recoverySettings,
        IAuditLogger auditLogger)
    {
        _userManager = userManager;
        _context = context;
        _passwordPolicy = passwordPolicy.Value;
        _recoverySettings = recoverySettings.Value;
        _auditLogger = auditLogger;
    }

    [BindProperty]
    public ResetPassword Input { get; set; } = new();

    public IActionResult OnGet(string? email, string? token)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(token))
        {
            return RedirectToPage("/Login");
        }

        Input.Email = email;
        Input.Token = token;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = Request.Headers.UserAgent.ToString();

        var user = await _userManager.FindByEmailAsync(Input.Email.Trim());
        if (user == null)
        {
            await _auditLogger.LogAuthEventAsync(
                null,
                AuditEventType.PasswordResetFailed,
                "Password reset user not found",
                ipAddress,
                userAgent);
            return RedirectToPage("/ResetPasswordConfirmation");
        }

        var historyCount = Math.Max(_passwordPolicy.PasswordHistoryCount, 0);
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
                var verify = _userManager.PasswordHasher.VerifyHashedPassword(user, hash, Input.Password);
                if (verify == PasswordVerificationResult.Success)
                {
                    ModelState.AddModelError("", "You cannot reuse a recent password.");
                    await _auditLogger.LogAuthEventAsync(
                        user.Id,
                        AuditEventType.PasswordResetFailed,
                        "Password reuse blocked",
                        ipAddress,
                        userAgent);
                    return Page();
                }
            }
        }

        var decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(Input.Token));
        var result = await _userManager.ResetPasswordAsync(user, decodedToken, Input.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            await _auditLogger.LogAuthEventAsync(
                user.Id,
                AuditEventType.PasswordResetFailed,
                "Reset password failed",
                ipAddress,
                userAgent);
            return Page();
        }

        var now = DateTime.UtcNow;
        user.PasswordLastChangedAt = now;
        if (_passwordPolicy.MaxPasswordAgeDays > 0)
        {
            user.PasswordExpiresAt = now.AddDays(_passwordPolicy.MaxPasswordAgeDays);
        }

        await _userManager.UpdateAsync(user);
        await _userManager.ResetAccessFailedCountAsync(user);

        _context.PasswordHistories.Add(new PasswordHistory
        {
            UserId = user.Id,
            PasswordHash = user.PasswordHash ?? string.Empty,
            CreatedAt = now
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

        await _auditLogger.LogAuthEventAsync(
            user.Id,
            AuditEventType.PasswordResetSucceeded,
            "Password reset succeeded",
            ipAddress,
            userAgent);

        return RedirectToPage("/ResetPasswordConfirmation");
    }
}
