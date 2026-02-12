using System.ComponentModel.DataAnnotations;

namespace WebApplication3.ViewModels;

public class Enable2fa
{
    [Display(Name = "Verification Code")]
    public string? Code { get; set; }
}
