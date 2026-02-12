using System.ComponentModel.DataAnnotations;

namespace WebApplication3.ViewModels;

public class LoginWith2fa
{
    [Required]
    [Display(Name = "Verification Code")]
    public string Code { get; set; } = string.Empty;

    public bool RememberMachine { get; set; }
    public bool RememberMe { get; set; }
}
