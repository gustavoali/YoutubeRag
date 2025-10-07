namespace YoutubeRag.Domain.Entities;

public class User : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
    public bool IsEmailVerified { get; set; } = false;
    public string? EmailVerificationToken { get; set; }
    public DateTime? EmailVerifiedAt { get; set; }

    // OAuth Fields
    public string? GoogleId { get; set; }
    public string? GoogleRefreshToken { get; set; }

    // Profile
    public string? Avatar { get; set; }
    public string? Bio { get; set; }
    public DateTime? LastLoginAt { get; set; }

    // Account Security
    public int FailedLoginAttempts { get; set; } = 0;
    public DateTime? LockoutEndDate { get; set; }

    // Navigation Properties
    public virtual ICollection<Video> Videos { get; set; } = new List<Video>();
    public virtual ICollection<Job> Jobs { get; set; } = new List<Job>();
    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}