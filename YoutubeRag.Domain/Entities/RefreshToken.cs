namespace YoutubeRag.Domain.Entities;

public class RefreshToken : BaseEntity
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public bool IsRevoked { get; set; } = false;
    public DateTime? RevokedAt { get; set; }
    public string? RevokedReason { get; set; }
    public string? DeviceInfo { get; set; }
    public string? IpAddress { get; set; }

    // Foreign Key
    public string UserId { get; set; } = string.Empty;

    // Navigation Properties
    public virtual User User { get; set; } = null!;

    public bool IsActive => !IsRevoked && ExpiresAt > DateTime.UtcNow;
}