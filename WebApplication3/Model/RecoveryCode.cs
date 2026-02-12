using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication3.Model;

public class RecoveryCode
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [StringLength(450)]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [StringLength(256)]
    public string CodeHash { get; set; } = string.Empty;

    public bool IsUsed { get; set; }

    public DateTime? UsedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [StringLength(50)]
    public string? UsedFromIp { get; set; }

    [ForeignKey("UserId")]
    public ApplicationUser? User { get; set; }
}
