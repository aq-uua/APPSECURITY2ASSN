using System.ComponentModel.DataAnnotations;

namespace WebApplication3.ViewModels;

public class RecoveryCodeLogin
{
    [Required]
    [Display(Name = "Recovery Code")]
    [RegularExpression(@"^[A-Za-z0-9-]{8,16}$", ErrorMessage = "Enter a valid recovery code.")]
    public string RecoveryCode { get; set; } = string.Empty;

    public bool RememberMe { get; set; }
    public bool RememberMachine { get; set; }
}
