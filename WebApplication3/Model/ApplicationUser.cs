using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace WebApplication3.Model
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [StringLength(500)]
        public string? CreditCardEncrypted { get; set; }

        [Required]
        public GenderType Gender { get; set; }

        [Required]
        [StringLength(20)]
        public string MobileNumber { get; set; } = string.Empty;

        [StringLength(500)]
        public string? DeliveryAddressEncrypted { get; set; }

        [StringLength(500)]
        public string? AboutMe { get; set; }

        [StringLength(500)]
        public string? PhotoUrl { get; set; }

        [Required]
        public bool EmailVerified { get; set; } = false;

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public string? EmailVerificationToken { get; set; }

        public DateTime? EmailVerificationTokenExpiry { get; set; }

        public DateTime? PasswordLastChangedAt { get; set; }

        public DateTime? PasswordExpiresAt { get; set; }
    }

    public enum GenderType
    {
        Male,
        Female,
        Other,
        PreferNotToSay
    }
}
