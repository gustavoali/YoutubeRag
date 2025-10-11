using FluentAssertions;
using YoutubeRag.Domain.Entities;

namespace YoutubeRag.Tests.Unit.Domain.Entities;

/// <summary>
/// Unit tests for User entity.
/// Tests entity properties, defaults, and security fields.
/// </summary>
public class UserTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaultValues()
    {
        // Act
        var user = new User();

        // Assert
        user.Name.Should().Be(string.Empty);
        user.Email.Should().Be(string.Empty);
        user.PasswordHash.Should().Be(string.Empty);
        user.IsActive.Should().BeTrue();
        user.IsEmailVerified.Should().BeFalse();
        user.FailedLoginAttempts.Should().Be(0);
        user.Videos.Should().NotBeNull().And.BeEmpty();
        user.Jobs.Should().NotBeNull().And.BeEmpty();
        user.RefreshTokens.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void Name_CanBeSet()
    {
        // Arrange
        var user = new User();
        var name = "John Doe";

        // Act
        user.Name = name;

        // Assert
        user.Name.Should().Be(name);
    }

    [Fact]
    public void Email_CanBeSet()
    {
        // Arrange
        var user = new User();
        var email = "john.doe@example.com";

        // Act
        user.Email = email;

        // Assert
        user.Email.Should().Be(email);
    }

    [Fact]
    public void PasswordHash_CanBeSet()
    {
        // Arrange
        var user = new User();
        var passwordHash = "hashed_password_value";

        // Act
        user.PasswordHash = passwordHash;

        // Assert
        user.PasswordHash.Should().Be(passwordHash);
    }

    [Fact]
    public void IsActive_DefaultsToTrue()
    {
        // Act
        var user = new User();

        // Assert
        user.IsActive.Should().BeTrue();
    }

    [Fact]
    public void IsActive_CanBeSetToFalse()
    {
        // Arrange
        var user = new User();

        // Act
        user.IsActive = false;

        // Assert
        user.IsActive.Should().BeFalse();
    }

    [Fact]
    public void IsEmailVerified_DefaultsToFalse()
    {
        // Act
        var user = new User();

        // Assert
        user.IsEmailVerified.Should().BeFalse();
    }

    [Fact]
    public void EmailVerification_CanBeSet()
    {
        // Arrange
        var user = new User();
        var token = "verification_token_123";
        var verifiedAt = DateTime.UtcNow;

        // Act
        user.EmailVerificationToken = token;
        user.IsEmailVerified = true;
        user.EmailVerifiedAt = verifiedAt;

        // Assert
        user.EmailVerificationToken.Should().Be(token);
        user.IsEmailVerified.Should().BeTrue();
        user.EmailVerifiedAt.Should().Be(verifiedAt);
    }

    [Fact]
    public void GoogleAuth_FieldsCanBeSet()
    {
        // Arrange
        var user = new User();
        var googleId = "google_user_id_123";
        var refreshToken = "google_refresh_token";

        // Act
        user.GoogleId = googleId;
        user.GoogleRefreshToken = refreshToken;

        // Assert
        user.GoogleId.Should().Be(googleId);
        user.GoogleRefreshToken.Should().Be(refreshToken);
    }

    [Fact]
    public void Profile_FieldsCanBeSet()
    {
        // Arrange
        var user = new User();
        var avatar = "https://example.com/avatar.png";
        var bio = "Software developer";

        // Act
        user.Avatar = avatar;
        user.Bio = bio;

        // Assert
        user.Avatar.Should().Be(avatar);
        user.Bio.Should().Be(bio);
    }

    [Fact]
    public void LastLoginAt_CanBeSet()
    {
        // Arrange
        var user = new User();
        var lastLogin = DateTime.UtcNow;

        // Act
        user.LastLoginAt = lastLogin;

        // Assert
        user.LastLoginAt.Should().Be(lastLogin);
    }

    [Fact]
    public void FailedLoginAttempts_DefaultsToZero()
    {
        // Act
        var user = new User();

        // Assert
        user.FailedLoginAttempts.Should().Be(0);
    }

    [Fact]
    public void FailedLoginAttempts_CanBeIncremented()
    {
        // Arrange
        var user = new User();

        // Act
        user.FailedLoginAttempts = 3;

        // Assert
        user.FailedLoginAttempts.Should().Be(3);
    }

    [Fact]
    public void LockoutEndDate_CanBeSet()
    {
        // Arrange
        var user = new User();
        var lockoutEnd = DateTime.UtcNow.AddMinutes(15);

        // Act
        user.LockoutEndDate = lockoutEnd;

        // Assert
        user.LockoutEndDate.Should().Be(lockoutEnd);
    }

    [Fact]
    public void AccountSecurity_CanSimulateLockout()
    {
        // Arrange
        var user = new User();
        var lockoutEnd = DateTime.UtcNow.AddMinutes(30);

        // Act
        user.FailedLoginAttempts = 5;
        user.LockoutEndDate = lockoutEnd;

        // Assert
        user.FailedLoginAttempts.Should().Be(5);
        user.LockoutEndDate.Should().Be(lockoutEnd);
        user.LockoutEndDate.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public void InheritsFromBaseEntity()
    {
        // Act
        var user = new User();

        // Assert
        user.Should().BeAssignableTo<BaseEntity>();
        user.Id.Should().NotBeNullOrEmpty();
        user.CreatedAt.Should().NotBe(default(DateTime));
        user.UpdatedAt.Should().NotBe(default(DateTime));
    }
}
