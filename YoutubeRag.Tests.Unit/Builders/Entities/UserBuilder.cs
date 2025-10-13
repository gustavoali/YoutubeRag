using YoutubeRag.Domain.Entities;

namespace YoutubeRag.Tests.Unit.Builders.Entities;

/// <summary>
/// Builder for creating User test instances
/// </summary>
public class UserBuilder
{
    private string _id = Guid.NewGuid().ToString();
    private string _name = "Test User";
    private string _email = "test@example.com";
    private string _passwordHash = "$2a$11$hashed_password_example";
    private bool _isEmailVerified = true;
    private bool _isActive = true;
    private DateTime _createdAt = DateTime.UtcNow;
    private DateTime? _lastLoginAt = null;
    private int _failedLoginAttempts = 0;
    private DateTime? _lockoutEndDate = null;

    public UserBuilder WithId(string id)
    {
        _id = id;
        return this;
    }

    public UserBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public UserBuilder WithEmail(string email)
    {
        _email = email;
        return this;
    }

    public UserBuilder WithPasswordHash(string passwordHash)
    {
        _passwordHash = passwordHash;
        return this;
    }

    public UserBuilder WithIsEmailVerified(bool verified = true)
    {
        _isEmailVerified = verified;
        return this;
    }

    public UserBuilder WithFailedLoginAttempts(int attempts)
    {
        _failedLoginAttempts = attempts;
        return this;
    }

    public UserBuilder WithLockoutEndDate(DateTime? lockoutEndDate)
    {
        _lockoutEndDate = lockoutEndDate;
        return this;
    }

    public UserBuilder WithIsActive(bool isActive = true)
    {
        _isActive = isActive;
        return this;
    }

    public UserBuilder WithCreatedAt(DateTime createdAt)
    {
        _createdAt = createdAt;
        return this;
    }

    public UserBuilder WithLastLoginAt(DateTime? lastLoginAt)
    {
        _lastLoginAt = lastLoginAt;
        return this;
    }

    public User Build()
    {
        return new User
        {
            Id = _id,
            Name = _name,
            Email = _email,
            PasswordHash = _passwordHash,
            IsEmailVerified = _isEmailVerified,
            IsActive = _isActive,
            CreatedAt = _createdAt,
            LastLoginAt = _lastLoginAt,
            FailedLoginAttempts = _failedLoginAttempts,
            LockoutEndDate = _lockoutEndDate
        };
    }

    /// <summary>
    /// Creates a valid active User with default values
    /// </summary>
    public static User CreateValid() => new UserBuilder().Build();

    /// <summary>
    /// Creates an inactive User
    /// </summary>
    public static User CreateInactive() =>
        new UserBuilder().WithIsActive(false).Build();

    /// <summary>
    /// Creates a User with unverified email
    /// </summary>
    public static User CreateWithUnverifiedEmail() =>
        new UserBuilder().WithIsEmailVerified(false).Build();

    /// <summary>
    /// Creates a User who recently logged in
    /// </summary>
    public static User CreateRecentlyLoggedIn() =>
        new UserBuilder().WithLastLoginAt(DateTime.UtcNow.AddMinutes(-5)).Build();
}
