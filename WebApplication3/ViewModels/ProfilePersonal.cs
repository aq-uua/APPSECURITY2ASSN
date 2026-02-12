using System.ComponentModel.DataAnnotations;
using WebApplication3.Model;

namespace WebApplication3.ViewModels;

public class ProfilePersonal
{
    [Required]
    [StringLength(100)]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Gender")]
    public GenderType Gender { get; set; }

    [Required]
    [Phone]
    [StringLength(20)]
    [Display(Name = "Mobile Number")]
    public string MobileNumber { get; set; } = string.Empty;

    [StringLength(500)]
    [Display(Name = "About Me")]
    public string? AboutMe { get; set; }
}
