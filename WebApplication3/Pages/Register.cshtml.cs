using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Cryptography;
using System.Text.Encodings.Web;
using WebApplication3.Model;
using WebApplication3.Services;
using WebApplication3.ViewModels;

namespace WebApplication3.Pages
{
    public class RegisterModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IWebHostEnvironment _environment;
        private readonly IEmailSender _emailSender;
        private readonly IRazorViewToStringRenderer _renderer;
        private readonly AuthDbContext _context;
        private readonly PasswordPolicySettings _passwordPolicy;
        private readonly IEncryptionService _encryptionService;
        private readonly IRecaptchaVerifier _recaptchaVerifier;
        private readonly RecaptchaSettings _recaptchaSettings;
        private readonly IAuditLogger _auditLogger;
        private readonly ILogger<RegisterModel> _logger;

        [BindProperty]
        public Register RModel { get; set; } = new Register();

        public RegisterModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IWebHostEnvironment environment,
            IEmailSender emailSender,
            IRazorViewToStringRenderer renderer,
            AuthDbContext context,
            Microsoft.Extensions.Options.IOptions<PasswordPolicySettings> passwordPolicy,
            IEncryptionService encryptionService,
            IRecaptchaVerifier recaptchaVerifier,
            Microsoft.Extensions.Options.IOptions<RecaptchaSettings> recaptchaSettings,
            IAuditLogger auditLogger,
            ILogger<RegisterModel> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _environment = environment;
            _emailSender = emailSender;
            _logger = logger;
            _renderer = renderer;
            _context = context;
            _passwordPolicy = passwordPolicy.Value;
            _encryptionService = encryptionService;
            _recaptchaVerifier = recaptchaVerifier;
            _recaptchaSettings = recaptchaSettings.Value;
            _auditLogger = auditLogger;
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

            var recaptchaValid = await _recaptchaVerifier.VerifyAsync(RModel.RecaptchaToken, ipAddress);
            if (!recaptchaValid)
            {
                _logger.LogWarning("Recaptcha failed during registration.");
                await _auditLogger.LogAuthEventAsync(
                    null,
                    AuditEventType.RecaptchaFailed,
                    "Registration recaptcha failed",
                    ipAddress,
                    userAgent);
                ModelState.AddModelError(string.Empty, "Security verification failed. Please try again.");
                return Page();
            }

            // Check for duplicate email
            var existingUser = await _userManager.FindByEmailAsync(RModel.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError("RModel.Email", "An account with this email already exists. Please login or use a different email.");
                return Page();
            }

            // Handle photo upload
            string? photoUrl = null;
            if (RModel.Photo != null && RModel.Photo.Length > 0)
            {
                if (!string.Equals(RModel.Photo.ContentType, "image/jpeg", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(RModel.Photo.ContentType, "image/pjpeg", StringComparison.OrdinalIgnoreCase))
                {
                    ModelState.AddModelError("RModel.Photo", "Only JPG files are allowed.");
                    return Page();
                }

                var fileExtension = Path.GetExtension(RModel.Photo.FileName).ToLowerInvariant();
                if (fileExtension != ".jpg" && fileExtension != ".jpeg")
                {
                    ModelState.AddModelError("RModel.Photo", "Only JPG files are allowed.");
                    return Page();
                }

                await using var fileStream = RModel.Photo.OpenReadStream();
                var header = new byte[2];
                var readBytes = await fileStream.ReadAsync(header.AsMemory(0, 2));
                if (readBytes < 2 || header[0] != 0xFF || header[1] != 0xD8)
                {
                    ModelState.AddModelError("RModel.Photo", "Invalid JPEG file.");
                    return Page();
                }
                fileStream.Position = 0;

                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "photos");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // Generate safe filename
                var safeFileName = $"{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(uploadsFolder, safeFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await fileStream.CopyToAsync(stream);
                }

                photoUrl = $"/uploads/photos/{safeFileName}";
            }

            // Encrypt sensitive data
            string? encryptedCreditCard = null;
            string? encryptedAddress = null;

            if (!string.IsNullOrEmpty(RModel.CreditCard))
            {
                encryptedCreditCard = _encryptionService.Protect(RModel.CreditCard);
            }

            if (!string.IsNullOrEmpty(RModel.DeliveryAddress))
            {
                encryptedAddress = _encryptionService.Protect(RModel.DeliveryAddress);
            }

            // Create user
            var safeAboutMe = string.IsNullOrWhiteSpace(RModel.AboutMe)
                ? null
                : HtmlEncoder.Default.Encode(RModel.AboutMe);

            var user = new ApplicationUser
            {
                UserName = RModel.Email,
                Email = RModel.Email,
                FullName = RModel.FullName,
                Gender = RModel.Gender,
                MobileNumber = RModel.MobileNumber,
                DeliveryAddressEncrypted = encryptedAddress,
                CreditCardEncrypted = encryptedCreditCard,
                AboutMe = safeAboutMe,
                PhotoUrl = photoUrl,
                EmailVerified = false,
                CreatedAt = DateTime.UtcNow,
                EmailVerificationToken = GenerateVerificationToken(),
                EmailVerificationTokenExpiry = DateTime.UtcNow.AddHours(24)
            };

            var result = await _userManager.CreateAsync(user, RModel.Password);

            if (result.Succeeded)
            {
                _logger.LogInformation("User created a new account with password.");

                var now = DateTime.UtcNow;
                user.PasswordLastChangedAt = now;
                if (_passwordPolicy.MaxPasswordAgeDays > 0)
                {
                    user.PasswordExpiresAt = now.AddDays(_passwordPolicy.MaxPasswordAgeDays);
                }

                var updateResult = await _userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    foreach (var error in updateResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }

                    return Page();
                }

                _context.PasswordHistories.Add(new PasswordHistory
                {
                    UserId = user.Id,
                    PasswordHash = user.PasswordHash ?? string.Empty,
                    CreatedAt = now
                });

                await _context.SaveChangesAsync();

                // Send email verification
                await SendEmailVerificationAsync(user);

                // Don't sign in automatically - require email verification first
                return RedirectToPage("VerifyEmail", new { email = user.Email });
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return Page();
        }

        private string GenerateVerificationToken()
        {
            var tokenBytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(tokenBytes);
            }
            return Convert.ToBase64String(tokenBytes);
        }

        private async Task SendEmailVerificationAsync(ApplicationUser user)
        {
            if (string.IsNullOrWhiteSpace(user.Email))
            {
                _logger.LogWarning("Verification email skipped: user email missing.");
                return;
            }

            var callbackUrl = Url.Page(
                "/VerifyEmail",
                pageHandler: null,
                values: new { email = user.Email, token = user.EmailVerificationToken },
                protocol: Request.Scheme);

            var subject = "Verify your email - Farm Fresh Market";
            var viewPath = "~/Services/EmailTemplates/VerificationEmailStyled.cshtml";
            var html = await _renderer.RenderViewToStringAsync(viewPath, callbackUrl);

            await _emailSender.SendEmailAsync(user.Email, subject, html);
        }
    }
}
