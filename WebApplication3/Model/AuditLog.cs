using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace WebApplication3.Model;

public class AuditLog
{
    [Key]
    public Guid Id { get; set; }

    public string? UserId { get; set; }

    [Required]
    [StringLength(50)]
    public string EventType { get; set; } = string.Empty;

    [StringLength(100)]
    public string? EventDescription { get; set; }

    public string? EventData { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [StringLength(50)]
    public string? IpAddress { get; set; }

    [StringLength(500)]
    public string? UserAgent { get; set; }

    [StringLength(255)]
    public string? SessionId { get; set; }

    [ForeignKey(nameof(UserId))]
    public virtual ApplicationUser? User { get; set; }

    public void SetEventData(object data)
    {
        EventData = JsonSerializer.Serialize(data, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }

    public T? GetEventData<T>()
    {
        if (string.IsNullOrEmpty(EventData))
            return default;
        
        return JsonSerializer.Deserialize<T>(EventData, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }
}
