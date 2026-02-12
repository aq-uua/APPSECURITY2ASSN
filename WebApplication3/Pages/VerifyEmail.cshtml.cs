using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebApplication3.Model;

namespace WebApplication3.Pages
{
    public class VerifyEmailModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<VerifyEmailModel> _logger;

        public VerifyEmailModel(UserManager<ApplicationUser> userManager, ILogger<VerifyEmailModel> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }

        [BindProperty(SupportsGet = true)]
        public string? Email { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Token { get; set; }

        public bool IsSuccess { get; set; }
        public bool IsError { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrEmpty(Email) || string.IsNullOrEmpty(Token))
            {
                IsError = true;
                ErrorMessage = "Invalid verification link. Please check your email and try again.";
                return Page();
            }

            var user = await _userManager.FindByEmailAsync(Email);
            if (user == null)
            {
                IsError = true;
                ErrorMessage = "User not found. Please register again.";
                return Page();
            }

            if (user.EmailVerified)
            {
                IsSuccess = true;
                return Page();
            }

            if (user.EmailVerificationTokenExpiry < DateTime.UtcNow)
            {
                IsError = true;
                ErrorMessage = "Verification link has expired. Please request a new one.";
                return Page();
            }

            if (user.EmailVerificationToken != Token)
            {
                IsError = true;
                ErrorMessage = "Invalid verification token. Please request a new verification email.";
                return Page();
            }

            // Mark email as verified
            user.EmailVerified = true;
            user.EmailConfirmed = true;
            user.EmailVerificationToken = null;
            user.EmailVerificationTokenExpiry = null;
            user.UpdatedAt = DateTime.UtcNow;

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                _logger.LogInformation("Email verified for user: {Email}", Email);
                IsSuccess = true;
            }
            else
            {
                IsError = true;
                ErrorMessage = "An error occurred while verifying your email. Please try again.";
            }

            return Page();
        }
    }
}
