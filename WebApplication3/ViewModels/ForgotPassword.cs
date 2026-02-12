using System.ComponentModel.DataAnnotations;

namespace WebApplication3.ViewModels;

public class ForgotPassword
{
    [Required]
    [EmailAddress]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    public string? RecaptchaToken { get; set; }
}
