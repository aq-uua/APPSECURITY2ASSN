using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebApplication3.Model;
using Microsoft.Extensions.Options;
using WebApplication3.Services;
using WebApplication3.ViewModels;

namespace WebApplication3.Pages;

[Authorize]
public class ProfileModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IWebHostEnvironment _environment;
    private readonly IEncryptionService _encryptionService;
    private readonly IAuditLogger _auditLogger;
    private readonly IRecoveryCodeService _recoveryCodeService;
    private readonly ILogger<ProfileModel> _logger;
    private readonly PasswordPolicySettings _passwordPolicy;

    public ProfileModel(
        UserManager<ApplicationUser> userManager,
        IWebHostEnvironment environment,
        IEncryptionService encryptionService,
        IAuditLogger auditLogger,
        IRecoveryCodeService recoveryCodeService,
        ILogger<ProfileModel> logger,
        IOptions<PasswordPolicySettings> passwordPolicy)
    {
        _userManager = userManager;
        _environment = environment;
        _encryptionService = encryptionService;
        _auditLogger = auditLogger;
        _recoveryCodeService = recoveryCodeService;
        _logger = logger;
        _passwordPolicy = passwordPolicy.Value;
    }

    [BindProperty]
    public ProfilePersonal Personal { get; set; } = new();

    [BindProperty]
    public ProfileDelivery Delivery { get; set; } = new();

    [BindProperty]
    public ProfilePayment Payment { get; set; } = new();

    [BindProperty]
    public ProfilePhoto Photo { get; set; } = new();

    public string Email { get; private set; } = string.Empty;
    public bool IsTwoFactorEnabled { get; private set; }
    public string[]? RecoveryCodes { get; private set; }
    public bool ShowPasswordExpiryAlert { get; private set; }

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return RedirectToPage("/Login");
        }

        await LoadAsync(user);
        ShowPasswordExpiryAlert = _passwordPolicy.IsPasswordNearExpiry(user.PasswordLastChangedAt);
        return Page();
    }

    public async Task<IActionResult> OnPostPersonalAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return RedirectToPage("/Login");
        }

        ModelState.Clear();
        if (!TryValidateModel(Personal, nameof(Personal)))
        {
            await LoadAsync(user);
            return Page();
        }

        user.FullName = Personal.FullName;
        user.Gender = Personal.Gender;
        user.MobileNumber = Personal.MobileNumber;
        user.AboutMe = string.IsNullOrWhiteSpace(Personal.AboutMe)
            ? null
            : HtmlEncoder.Default.Encode(Personal.AboutMe);
        user.UpdatedAt = DateTime.UtcNow;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            await LoadAsync(user);
            return Page();
        }

        await _auditLogger.LogAuthEventAsync(
            user.Id,
            AuditEventType.ProfileUpdated,
            "Profile personal information updated",
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString());

        StatusMessage = "Personal information updated.";
        await LoadAsync(user);
        return Page();
    }

    public async Task<IActionResult> OnPostDeliveryAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return RedirectToPage("/Login");
        }

        ModelState.Clear();
        if (!TryValidateModel(Delivery, nameof(Delivery)))
        {
            await LoadAsync(user);
            return Page();
        }

        user.DeliveryAddressEncrypted = string.IsNullOrWhiteSpace(Delivery.DeliveryAddress)
            ? null
            : _encryptionService.Protect(Delivery.DeliveryAddress);
        user.UpdatedAt = DateTime.UtcNow;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            await LoadAsync(user);
            return Page();
        }

        await _auditLogger.LogAuthEventAsync(
            user.Id,
            AuditEventType.DeliveryUpdated,
            "Delivery address updated",
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString());

        StatusMessage = "Delivery address updated.";
        await LoadAsync(user);
        return Page();
    }

    public async Task<IActionResult> OnPostPaymentAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return RedirectToPage("/Login");
        }

        ModelState.Clear();
        if (!TryValidateModel(Payment, nameof(Payment)))
        {
            await LoadAsync(user);
            return Page();
        }

        if (string.IsNullOrWhiteSpace(Payment.CreditCard))
        {
            ModelState.AddModelError("Payment.CreditCard", "Enter a credit card number.");
            await LoadAsync(user);
            return Page();
        }

        user.CreditCardEncrypted = _encryptionService.Protect(Payment.CreditCard);
        user.UpdatedAt = DateTime.UtcNow;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            await LoadAsync(user);
            return Page();
        }

        await _auditLogger.LogAuthEventAsync(
            user.Id,
            AuditEventType.PaymentUpdated,
            "Payment method updated",
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString());

        StatusMessage = "Payment method updated.";
        await LoadAsync(user);
        return Page();
    }

    public async Task<IActionResult> OnPostPhotoAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return RedirectToPage("/Login");
        }

        ModelState.Clear();
        if (!TryValidateModel(Photo, nameof(Photo)))
        {
            await LoadAsync(user);
            return Page();
        }

        if (Photo.Photo == null || Photo.Photo.Length == 0)
        {
            ModelState.AddModelError("Photo.Photo", "Select a JPG file.");
            await LoadAsync(user);
            return Page();
        }

        if (!string.Equals(Photo.Photo.ContentType, "image/jpeg", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(Photo.Photo.ContentType, "image/pjpeg", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError("Photo.Photo", "Only JPG files are allowed.");
            await LoadAsync(user);
            return Page();
        }

        var extension = Path.GetExtension(Photo.Photo.FileName).ToLowerInvariant();
        if (extension != ".jpg" && extension != ".jpeg")
        {
            ModelState.AddModelError("Photo.Photo", "Only JPG files are allowed.");
            await LoadAsync(user);
            return Page();
        }

        await using var fileStream = Photo.Photo.OpenReadStream();
        var header = new byte[2];
        var readBytes = await fileStream.ReadAsync(header.AsMemory(0, 2));
        if (readBytes < 2 || header[0] != 0xFF || header[1] != 0xD8)
        {
            ModelState.AddModelError("Photo.Photo", "Invalid JPEG file.");
            await LoadAsync(user);
            return Page();
        }
        fileStream.Position = 0;

        var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "photos");
        if (!Directory.Exists(uploadsFolder))
        {
            Directory.CreateDirectory(uploadsFolder);
        }

        var safeFileName = $"{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(uploadsFolder, safeFileName);
        await using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await fileStream.CopyToAsync(stream);
        }

        user.PhotoUrl = $"/uploads/photos/{safeFileName}";
        user.UpdatedAt = DateTime.UtcNow;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            await LoadAsync(user);
            return Page();
        }

        await _auditLogger.LogAuthEventAsync(
            user.Id,
            AuditEventType.PhotoUpdated,
            "Profile photo updated",
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString());

        StatusMessage = "Profile photo updated.";
        await LoadAsync(user);
        return Page();
    }

    public async Task<IActionResult> OnPostEnable2faAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return RedirectToPage("/Login");
        }

        await _userManager.SetTwoFactorEnabledAsync(user, true);
        await _auditLogger.LogAuthEventAsync(
            user.Id,
            AuditEventType.TwoFactorEnabled,
            "Two-factor enabled",
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString());

        StatusMessage = "Two-factor authentication enabled.";
        await LoadAsync(user);
        return Page();
    }

    public async Task<IActionResult> OnPostDisable2faAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return RedirectToPage("/Login");
        }

        await _userManager.SetTwoFactorEnabledAsync(user, false);
        await _auditLogger.LogAuthEventAsync(
            user.Id,
            AuditEventType.TwoFactorDisabled,
            "Two-factor disabled",
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString());

        StatusMessage = "Two-factor authentication disabled.";
        await LoadAsync(user);
        return Page();
    }

    public async Task<IActionResult> OnPostGenerateRecoveryCodesAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return RedirectToPage("/Login");
        }

        var isEnabled = await _userManager.GetTwoFactorEnabledAsync(user);
        if (!isEnabled)
        {
            StatusMessage = "Enable two-factor authentication before generating recovery codes.";
            await LoadAsync(user);
            return Page();
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var codes = await _recoveryCodeService.GenerateCodesAsync(user.Id, 8, ipAddress);
        RecoveryCodes = codes;

        await _auditLogger.LogAuthEventAsync(
            user.Id,
            AuditEventType.RecoveryCodesGenerated,
            "Recovery codes generated",
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString());

        StatusMessage = "New recovery codes generated.";
        await LoadAsync(user);
        return Page();
    }

    private async Task LoadAsync(ApplicationUser user)
    {
        Personal = new ProfilePersonal
        {
            FullName = user.FullName,
            Gender = user.Gender,
            MobileNumber = user.MobileNumber,
            AboutMe = user.AboutMe
        };

        Delivery = new ProfileDelivery
        {
            DeliveryAddress = UnprotectSafe(user.DeliveryAddressEncrypted)
        };

        var card = UnprotectSafe(user.CreditCardEncrypted);
        Payment = new ProfilePayment
        {
            MaskedCard = MaskCard(card)
        };

        Photo = new ProfilePhoto
        {
            PhotoUrl = user.PhotoUrl
        };

        Email = user.Email ?? string.Empty;
        IsTwoFactorEnabled = await _userManager.GetTwoFactorEnabledAsync(user);
    }

    private string UnprotectSafe(string? protectedValue)
    {
        if (string.IsNullOrWhiteSpace(protectedValue))
        {
            return string.Empty;
        }

        try
        {
            return _encryptionService.Unprotect(protectedValue);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to unprotect sensitive value");
            return string.Empty;
        }
    }

    private string MaskCard(string? cardNumber)
    {
        if (string.IsNullOrWhiteSpace(cardNumber))
        {
            return "No card on file";
        }

        var trimmed = new string(cardNumber.Where(char.IsDigit).ToArray());
        if (trimmed.Length < 4)
        {
            return "Card on file";
        }

        var last4 = trimmed[^4..];
        return $"**** **** **** {last4}";
    }
}
