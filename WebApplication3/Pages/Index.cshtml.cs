using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using WebApplication3.Model;

namespace WebApplication3.Pages
{
	[Authorize]
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly PasswordPolicySettings _passwordPolicy;

        public IndexModel(
            ILogger<IndexModel> logger,
            UserManager<ApplicationUser> userManager,
            IOptions<PasswordPolicySettings> passwordPolicy)
        {
            _logger = logger;
            _userManager = userManager;
            _passwordPolicy = passwordPolicy.Value;
        }

        public bool ShowPasswordExpiryAlert { get; private set; }

        public async Task OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                ShowPasswordExpiryAlert = _passwordPolicy.IsPasswordNearExpiry(user.PasswordLastChangedAt);
            }
        }
    }
}
