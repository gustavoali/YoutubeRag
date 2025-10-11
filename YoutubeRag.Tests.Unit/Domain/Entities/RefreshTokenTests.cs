using FluentAssertions;
using YoutubeRag.Domain.Entities;

namespace YoutubeRag.Tests.Unit.Domain.Entities;

/// <summary>
/// Unit tests for RefreshToken entity.
/// Tests entity properties, defaults, and IsActive computed property.
/// </summary>
public class RefreshTokenTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaultValues()
    {
        // Act
        var token = new RefreshToken();

        // Assert
        token.Token.Should().Be(string.Empty);
        token.ExpiresAt.Should().Be(default(DateTime));
        token.IsRevoked.Should().BeFalse();
        token.RevokedAt.Should().BeNull();
        token.RevokedReason.Should().BeNull();
        token.DeviceInfo.Should().BeNull();
        token.IpAddress.Should().BeNull();
        token.UserId.Should().Be(string.Empty);
    }

    [Fact]
    public void IsActive_ReturnsTrue_WhenNotRevokedAndNotExpired()
    {
        // Arrange
        var token = new RefreshToken
        {
            IsRevoked = false,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        // Act
        var isActive = token.IsActive;

        // Assert
        isActive.Should().BeTrue();
    }

    [Fact]
    public void IsActive_ReturnsFalse_WhenRevoked()
    {
        // Arrange
        var token = new RefreshToken
        {
            IsRevoked = true,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        // Act
        var isActive = token.IsActive;

        // Assert
        isActive.Should().BeFalse();
    }

    [Fact]
    public void IsActive_ReturnsFalse_WhenExpired()
    {
        // Arrange
        var token = new RefreshToken
        {
            IsRevoked = false,
            ExpiresAt = DateTime.UtcNow.AddDays(-1)
        };

        // Act
        var isActive = token.IsActive;

        // Assert
        isActive.Should().BeFalse();
    }

    [Fact]
    public void IsActive_ReturnsFalse_WhenRevokedAndExpired()
    {
        // Arrange
        var token = new RefreshToken
        {
            IsRevoked = true,
            ExpiresAt = DateTime.UtcNow.AddDays(-1)
        };

        // Act
        var isActive = token.IsActive;

        // Assert
        isActive.Should().BeFalse();
    }

    [Fact]
    public void IsActive_ReturnsFalse_WhenExpiresAtExactlyNow()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var token = new RefreshToken
        {
            IsRevoked = false,
            ExpiresAt = now
        };

        // Act
        var isActive = token.IsActive;

        // Assert - May be slightly before or after depending on timing
        // This is acceptable for the edge case
        isActive.Should().BeFalse();
    }

    [Fact]
    public void Token_CanBeSet()
    {
        // Arrange
        var refreshToken = new RefreshToken();
        var tokenValue = "refresh_token_abc123";

        // Act
        refreshToken.Token = tokenValue;

        // Assert
        refreshToken.Token.Should().Be(tokenValue);
    }

    [Fact]
    public void Revoke_CanBeSet()
    {
        // Arrange
        var token = new RefreshToken();
        var revokedAt = DateTime.UtcNow;
        var reason = "User logout";

        // Act
        token.IsRevoked = true;
        token.RevokedAt = revokedAt;
        token.RevokedReason = reason;

        // Assert
        token.IsRevoked.Should().BeTrue();
        token.RevokedAt.Should().Be(revokedAt);
        token.RevokedReason.Should().Be(reason);
    }

    [Fact]
    public void DeviceInfo_CanBeSet()
    {
        // Arrange
        var token = new RefreshToken();
        var deviceInfo = "Mozilla/5.0 (Windows NT 10.0; Win64; x64)";

        // Act
        token.DeviceInfo = deviceInfo;

        // Assert
        token.DeviceInfo.Should().Be(deviceInfo);
    }

    [Fact]
    public void IpAddress_CanBeSet()
    {
        // Arrange
        var token = new RefreshToken();
        var ipAddress = "192.168.1.100";

        // Act
        token.IpAddress = ipAddress;

        // Assert
        token.IpAddress.Should().Be(ipAddress);
    }

    [Fact]
    public void InheritsFromBaseEntity()
    {
        // Act
        var token = new RefreshToken();

        // Assert
        token.Should().BeAssignableTo<BaseEntity>();
        token.Id.Should().NotBeNullOrEmpty();
        token.CreatedAt.Should().NotBe(default(DateTime));
        token.UpdatedAt.Should().NotBe(default(DateTime));
    }
}
