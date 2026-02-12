using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using WebApplication3.Model;
using WebApplication3.Services;
using WebApplication3.ViewModels;

namespace WebApplication3.Pages
{
    public class LoginModel : PageModel
    {
        [BindProperty]
        public Login LModel { get; set; } = new Login();

        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<LoginModel> _logger;
        private readonly IAuditLogger _auditLogger;
        private readonly ISessionManager _sessionManager;
        private readonly IRecaptchaVerifier _recaptchaVerifier;
        private readonly RecaptchaSettings _recaptchaSettings;
        private readonly PasswordPolicySettings _passwordPolicy;

        public LoginModel(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            ILogger<LoginModel> logger,
            IAuditLogger auditLogger,
            ISessionManager sessionManager,
            IRecaptchaVerifier recaptchaVerifier,
            IOptions<RecaptchaSettings> recaptchaSettings,
            IOptions<PasswordPolicySettings> passwordPolicy)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
            _auditLogger = auditLogger;
            _sessionManager = sessionManager;
            _recaptchaVerifier = recaptchaVerifier;
            _recaptchaSettings = recaptchaSettings.Value;
            _passwordPolicy = passwordPolicy.Value;
        }

        public string RecaptchaSiteKey => _recaptchaSettings.SiteKey;
        public bool RecaptchaEnabled => _recaptchaSettings.Enabled;

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var userAgent = Request.Headers.UserAgent.ToString();

            var recaptchaValid = await _recaptchaVerifier.VerifyAsync(LModel.RecaptchaToken, ipAddress);
            if (!recaptchaValid)
            {
                await _auditLogger.LogAuthEventAsync(
                    null,
                    AuditEventType.RecaptchaFailed,
                    "Login recaptcha failed",
                    ipAddress,
                    userAgent);
                ModelState.AddModelError("", "Security verification failed. Please try again.");
                return Page();
            }

            var email = LModel.Email?.Trim();
            if (string.IsNullOrEmpty(email))
            {
                ModelState.AddModelError("", "Invalid login attempt.");
                return Page();
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                await _auditLogger.LogAuthEventAsync(
                    null,
                    AuditEventType.LoginFailure,
                    "User not found",
                    ipAddress,
                    userAgent);
                
                ModelState.AddModelError("", "Invalid login attempt.");
                return Page();
            }

            if (await _userManager.GetTwoFactorEnabledAsync(user))
            {
                await _signInManager.ForgetTwoFactorClientAsync();
            }

            if (!user.EmailConfirmed || !user.EmailVerified)
            {
                await _auditLogger.LogAuthEventAsync(
                    user.Id,
                    AuditEventType.LoginFailure,
                    "Email not verified",
                    ipAddress,
                    userAgent);
                
                ModelState.AddModelError("", "Please verify your email before logging in. Check your inbox for the verification link.");
                return Page();
            }

            var result = await _signInManager.PasswordSignInAsync(
                user,
                LModel.Password,
                LModel.RememberMe,
                lockoutOnFailure: true);

            if (result.RequiresTwoFactor)
            {
                return RedirectToPage("./LoginWith2fa", new { ReturnUrl = "~/", RememberMe = LModel.RememberMe });
            }

            if (result.Succeeded)
            {
                _logger.LogInformation("User logged in: {UserId}", user.Id);
                
                await _auditLogger.LogAuthEventAsync(
                    user.Id,
                    AuditEventType.LoginSuccess,
                    "Login successful",
                    ipAddress,
                    userAgent);

                var deviceInfo = GetDeviceInfo(userAgent);
                var session = await _sessionManager.CreateSessionAsync(
                    user.Id,
                    deviceInfo,
                    ipAddress);

                HttpContext.Session.SetString("AuthSessionId", session.Id.ToString());

                if (_passwordPolicy.IsPasswordExpired(user.PasswordLastChangedAt))
                {
                    await _auditLogger.LogAuthEventAsync(
                        user.Id,
                        AuditEventType.PasswordExpired,
                        "Password expired",
                        ipAddress,
                        userAgent);

                    TempData["StatusMessage"] = "Your password has expired. Please update it to continue.";
                    return RedirectToPage("/ChangePassword");
                }

                return RedirectToPage("Index");
            }

            if (result.IsLockedOut)
            {
                _logger.LogWarning("User account locked out: {Email}", LModel.Email);
                
                await _auditLogger.LogAuthEventAsync(
                    user.Id,
                    AuditEventType.LoginFailure,
                    "Account locked out",
                    ipAddress,
                    userAgent);
                
                return RedirectToPage("./Lockout");
            }

            await _auditLogger.LogAuthEventAsync(
                user.Id,
                AuditEventType.LoginFailure,
                "Invalid password",
                ipAddress,
                userAgent);

            ModelState.AddModelError("", "Invalid login attempt.");
            return Page();
        }

        private string GetDeviceInfo(string userAgent)
        {
            if (string.IsNullOrEmpty(userAgent))
                return "Unknown Device";

            if (userAgent.Contains("Mobile"))
                return "Mobile Device";
            if (userAgent.Contains("Tablet"))
                return "Tablet Device";
            
            return "Desktop Browser";
        }
    }
}
