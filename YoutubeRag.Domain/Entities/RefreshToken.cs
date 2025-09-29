using System.ComponentModel.DataAnnotations;

namespace YoutubeRag.Domain.Entities;

public class RefreshToken : BaseEntity
{
    [Required]
    public string Token { get; set; } = string.Empty;

    [Required]
    public DateTime ExpiresAt { get; set; }

    public bool IsRevoked { get; set; } = false;
    public DateTime? RevokedAt { get; set; }
    public string? RevokedReason { get; set; }

    [StringLength(255)]
    public string? DeviceInfo { get; set; }

    [StringLength(45)]
    public string? IpAddress { get; set; }

    // Foreign Key
    [Required]
    public string UserId { get; set; } = string.Empty;

    // Navigation Properties
    public virtual User User { get; set; } = null!;

    public bool IsActive => !IsRevoked && ExpiresAt > DateTime.UtcNow;
}