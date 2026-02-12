using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication3.Model;

public class AuthSession
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [StringLength(128)]
    public string SessionToken { get; set; } = string.Empty;

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastActiveAt { get; set; }

    [Required]
    public DateTime ExpiresAt { get; set; }

    public DateTime? TerminatedAt { get; set; }

    [StringLength(500)]
    public string? DeviceInfo { get; set; }

    [StringLength(50)]
    public string? IpAddress { get; set; }

    public bool IsActive { get; set; } = true;

    [ForeignKey(nameof(UserId))]
    public virtual ApplicationUser? User { get; set; }
}
