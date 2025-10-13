using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using YoutubeRag.Application.DTOs.Auth;
using YoutubeRag.Application.Exceptions;
using YoutubeRag.Application.Interfaces;
using YoutubeRag.Application.Services;
using YoutubeRag.Domain.Entities;
using YoutubeRag.Tests.Unit.Builders.Auth;
using YoutubeRag.Tests.Unit.Builders.Entities;

namespace YoutubeRag.Tests.Unit.Application.Services;

public class AuthServiceTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<ILogger<AuthService>> _mockLogger;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IRefreshTokenRepository> _mockRefreshTokenRepository;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockMapper = new Mock<IMapper>();
        _mockLogger = new Mock<ILogger<AuthService>>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockUserRepository = new Mock<IUserRepository>();
        _mockRefreshTokenRepository = new Mock<IRefreshTokenRepository>();

        // Setup UnitOfWork to return mock repositories
        _mockUnitOfWork.Setup(u => u.Users).Returns(_mockUserRepository.Object);
        _mockUnitOfWork.Setup(u => u.RefreshTokens).Returns(_mockRefreshTokenRepository.Object);

        // Setup JwtSettings section
        var mockJwtSettingsSection = new Mock<IConfigurationSection>();
        mockJwtSettingsSection.Setup(s => s["SecretKey"])
            .Returns("ThisIsAVerySecretKeyThatIsAtLeast32CharactersLong!");
        mockJwtSettingsSection.Setup(s => s["ExpiryMinutes"]).Returns("60");
        mockJwtSettingsSection.Setup(s => s["RefreshTokenExpiryDays"]).Returns("30");

        _mockConfiguration.Setup(c => c.GetSection("JwtSettings"))
            .Returns(mockJwtSettingsSection.Object);
        _mockConfiguration.Setup(c => c["JwtSettings:SecretKey"])
            .Returns("ThisIsAVerySecretKeyThatIsAtLeast32CharactersLong!");
        _mockConfiguration.Setup(c => c["JwtSettings:ExpiryMinutes"]).Returns("60");
        _mockConfiguration.Setup(c => c["JwtSettings:RefreshTokenExpiryDays"]).Returns("30");

        _authService = new AuthService(
            _mockUnitOfWork.Object,
            _mockMapper.Object,
            _mockLogger.Object,
            _mockConfiguration.Object);
    }

    [Fact]
    public async Task LoginAsync_WithNonExistentUser_ThrowsUnauthorizedException()
    {
        // Arrange
        var loginRequest = LoginRequestDtoBuilder.CreateValid();

        // Setup: User not found
        _mockUserRepository
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>()))
            .ReturnsAsync(new List<User>());

        // Act
        var act = async () => await _authService.LoginAsync(loginRequest);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("Invalid email or password");
    }

    [Fact]
    public async Task LoginAsync_WithInactiveUser_ThrowsUnauthorizedException()
    {
        // Arrange
        var loginRequest = LoginRequestDtoBuilder.CreateValid();
        var inactiveUser = UserBuilder.CreateInactive();

        // Use BCrypt to hash the password for proper verification
        inactiveUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!");

        _mockUserRepository
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>()))
            .ReturnsAsync(new List<User> { inactiveUser });

        // Act
        var act = async () => await _authService.LoginAsync(loginRequest);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("User account is inactive");
    }

    [Fact]
    public async Task LoginAsync_WithInvalidPassword_ThrowsUnauthorizedException()
    {
        // Arrange
        var loginRequest = new LoginRequestDtoBuilder()
            .WithPassword("WrongPassword123!")
            .Build();

        var user = UserBuilder.CreateValid();
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword123!");

        _mockUserRepository
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>()))
            .ReturnsAsync(new List<User> { user });

        _mockUnitOfWork
            .Setup(u => u.SaveChangesAsync(default))
            .ReturnsAsync(1);

        // Act
        var act = async () => await _authService.LoginAsync(loginRequest);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("Invalid email or password");
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsLoginResponse()
    {
        // Arrange
        var password = "Password123!";
        var loginRequest = new LoginRequestDtoBuilder()
            .WithPassword(password)
            .Build();

        var user = UserBuilder.CreateValid();
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);

        _mockUserRepository
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>()))
            .ReturnsAsync(new List<User> { user });

        _mockUnitOfWork
            .Setup(u => u.SaveChangesAsync(default))
            .ReturnsAsync(1);

        // Act
        var result = await _authService.LoginAsync(loginRequest);

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBeNullOrEmpty();
        result.ExpiresIn.Should().BeGreaterThan(0);

        // Verify SaveChangesAsync was called (to save user and refresh token)
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(default), Times.Once());
    }

    [Fact]
    public async Task LoginAsync_With5FailedAttempts_LocksAccount()
    {
        // Arrange
        var loginRequest = LoginRequestDtoBuilder.CreateValid();

        var user = UserBuilder.CreateValid();
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword");
        user.FailedLoginAttempts = 4; // Next attempt will be 5th

        _mockUserRepository
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>()))
            .ReturnsAsync(new List<User> { user });

        _mockUnitOfWork
            .Setup(u => u.SaveChangesAsync(default))
            .ReturnsAsync(1);

        // Act
        var act = async () => await _authService.LoginAsync(loginRequest);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedException>();
        user.FailedLoginAttempts.Should().Be(5);
        user.LockoutEndDate.Should().NotBeNull();
        user.LockoutEndDate.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task LoginAsync_WithLockedAccount_ThrowsUnauthorizedException()
    {
        // Arrange
        var loginRequest = LoginRequestDtoBuilder.CreateValid();

        var user = UserBuilder.CreateValid();
        user.LockoutEndDate = DateTime.UtcNow.AddMinutes(10); // Still locked

        _mockUserRepository
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>()))
            .ReturnsAsync(new List<User> { user });

        // Act
        var act = async () => await _authService.LoginAsync(loginRequest);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("*locked*");
    }
}
