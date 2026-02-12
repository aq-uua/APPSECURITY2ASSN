using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebApplication3.Model;
using WebApplication3.Services;

namespace WebApplication3.Pages;

public class LogoutModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ILogger<LogoutModel> _logger;
    private readonly IAuditLogger _auditLogger;

    public LogoutModel(
        SignInManager<ApplicationUser> signInManager,
        ILogger<LogoutModel> logger,
        IAuditLogger auditLogger)
    {
        _signInManager = signInManager;
        _logger = logger;
        _auditLogger = auditLogger;
    }

    public void OnGet() { }

    public async Task<IActionResult> OnPostLogoutAsync()
    {
        var userId = User?.Identity?.Name ?? "anonymous";
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = Request.Headers.UserAgent.ToString();

        await _auditLogger.LogAuthEventAsync(
            userId,
            AuditEventType.Logout,
            "User initiated logout",
            ipAddress,
            userAgent);

        await _signInManager.SignOutAsync();
        _logger.LogInformation("User logged out");
        
        return RedirectToPage("Login");
    }

    public IActionResult OnPostDontLogoutAsync()
    {
        return RedirectToPage("Index");
    }
}