using System.ComponentModel.DataAnnotations;

namespace WebApplication3.ViewModels;

public class ProfilePhoto
{
    [Display(Name = "Profile Photo")]
    [AllowedExtensions(new[] { ".jpg", ".jpeg" }, ErrorMessage = "Only JPG files are allowed")]
    [MaxFileSize(5 * 1024 * 1024, ErrorMessage = "File size cannot exceed 5MB")]
    public IFormFile? Photo { get; set; }

    public string? PhotoUrl { get; set; }
}
