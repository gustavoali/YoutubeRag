using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using YoutubeRag.Application.DTOs.User;
using YoutubeRag.Application.Exceptions;
using YoutubeRag.Application.Interfaces;
using YoutubeRag.Application.Services;
using YoutubeRag.Domain.Entities;
using YoutubeRag.Domain.Enums;
using YoutubeRag.Tests.Unit.Builders.Entities;
using YoutubeRag.Tests.Unit.Builders.UserDtos;

namespace YoutubeRag.Tests.Unit.Application.Services;

public class UserServiceTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<ILogger<UserService>> _mockLogger;
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IVideoRepository> _mockVideoRepository;
    private readonly Mock<IJobRepository> _mockJobRepository;
    private readonly UserService _userService;

    public UserServiceTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockMapper = new Mock<IMapper>();
        _mockLogger = new Mock<ILogger<UserService>>();
        _mockUserRepository = new Mock<IUserRepository>();
        _mockVideoRepository = new Mock<IVideoRepository>();
        _mockJobRepository = new Mock<IJobRepository>();

        _mockUnitOfWork.Setup(u => u.Users).Returns(_mockUserRepository.Object);
        _mockUnitOfWork.Setup(u => u.Videos).Returns(_mockVideoRepository.Object);
        _mockUnitOfWork.Setup(u => u.Jobs).Returns(_mockJobRepository.Object);

        _userService = new UserService(
            _mockUnitOfWork.Object,
            _mockMapper.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task GetByIdAsync_WithExistingUser_ReturnsUserDto()
    {
        // Arrange
        var userId = "user-123";
        var user = UserBuilder.CreateValid();
        var userDto = new UserDto { Id = userId, Name = "Test User" };

        _mockUserRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
        _mockMapper.Setup(m => m.Map<UserDto>(user)).Returns(userDto);

        // Act
        var result = await _userService.GetByIdAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result.Should().Be(userDto);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistentUser_ReturnsNull()
    {
        // Arrange
        var userId = "non-existent";
        _mockUserRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync((User?)null);

        // Act
        var result = await _userService.GetByIdAsync(userId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByEmailAsync_WithExistingEmail_ReturnsUserDto()
    {
        // Arrange
        var email = "test@example.com";
        var user = UserBuilder.CreateValid();
        var userDto = new UserDto { Email = email };

        _mockUserRepository
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>()))
            .ReturnsAsync(new List<User> { user });
        _mockMapper.Setup(m => m.Map<UserDto>(user)).Returns(userDto);

        // Act
        var result = await _userService.GetByEmailAsync(email);

        // Assert
        result.Should().NotBeNull();
        result!.Email.Should().Be(email);
    }

    [Fact]
    public async Task GetByEmailAsync_WithNonExistentEmail_ReturnsNull()
    {
        // Arrange
        var email = "nonexistent@example.com";
        _mockUserRepository
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>()))
            .ReturnsAsync(new List<User>());

        // Act
        var result = await _userService.GetByEmailAsync(email);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsPaginatedUsers()
    {
        // Arrange
        var users = new List<User> { UserBuilder.CreateValid(), UserBuilder.CreateValid() };
        var userListDtos = new List<UserListDto>
        {
            new("1", "User 1", "user1@example.com", true, DateTime.UtcNow),
            new("2", "User 2", "user2@example.com", true, DateTime.UtcNow)
        };

        _mockUserRepository.Setup(r => r.CountAsync(null)).ReturnsAsync(2);
        _mockUserRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(users);
        _mockMapper
            .Setup(m => m.Map<List<UserListDto>>(It.IsAny<IEnumerable<User>>()))
            .Returns(userListDtos);

        // Act
        var result = await _userService.GetAllAsync(1, 10);

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(2);
        result.Items.Should().HaveCount(2);
        result.PageNumber.Should().Be(1);
        result.PageSize.Should().Be(10);
    }

    [Fact]
    public async Task CreateAsync_WithValidDto_CreatesUserSuccessfully()
    {
        // Arrange
        var createDto = CreateUserDtoBuilder.CreateValid();
        var user = UserBuilder.CreateValid();
        var userDto = new UserDto { Id = user.Id, Email = createDto.Email };

        _mockUserRepository
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>()))
            .ReturnsAsync(new List<User>()); // Email doesn't exist

        _mockMapper.Setup(m => m.Map<User>(createDto)).Returns(user);
        _mockMapper.Setup(m => m.Map<UserDto>(It.IsAny<User>())).Returns(userDto);
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        // Act
        var result = await _userService.CreateAsync(createDto);

        // Assert
        result.Should().NotBeNull();
        result.Email.Should().Be(createDto.Email);
        _mockUserRepository.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateEmail_ThrowsBusinessValidationException()
    {
        // Arrange
        var createDto = CreateUserDtoBuilder.CreateValid();
        var existingUser = UserBuilder.CreateValid();

        _mockUserRepository
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>()))
            .ReturnsAsync(new List<User> { existingUser }); // Email already exists

        // Act
        var act = async () => await _userService.CreateAsync(createDto);

        // Assert
        await act.Should().ThrowAsync<BusinessValidationException>();
    }

    [Fact]
    public async Task UpdateAsync_WithValidDto_UpdatesUserSuccessfully()
    {
        // Arrange
        var userId = "user-123";
        var updateDto = UpdateUserDtoBuilder.CreateWithNameUpdate();
        var user = UserBuilder.CreateValid();
        var userDto = new UserDto { Id = userId, Name = "Updated Name" };

        _mockUserRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
        _mockMapper.Setup(m => m.Map<UserDto>(It.IsAny<User>())).Returns(userDto);
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        // Act
        var result = await _userService.UpdateAsync(userId, updateDto);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Updated Name");
        _mockUserRepository.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithNonExistentUser_ThrowsEntityNotFoundException()
    {
        // Arrange
        var userId = "non-existent";
        var updateDto = UpdateUserDtoBuilder.CreateEmpty();

        _mockUserRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync((User?)null);

        // Act
        var act = async () => await _userService.UpdateAsync(userId, updateDto);

        // Assert
        await act.Should().ThrowAsync<EntityNotFoundException>()
            .WithMessage("*User*not found*");
    }

    [Fact]
    public async Task UpdateAsync_WithDuplicateEmail_ThrowsBusinessValidationException()
    {
        // Arrange
        var userId = "user-123";
        var newEmail = "existing@example.com";
        var updateDto = UpdateUserDtoBuilder.CreateWithEmailUpdate(newEmail);
        var user = UserBuilder.CreateValid();
        var existingUser = new UserBuilder().WithId("different-user-id").Build();

        _mockUserRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
        _mockUserRepository
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>()))
            .ReturnsAsync(new List<User> { existingUser }); // Email already used by another user

        // Act
        var act = async () => await _userService.UpdateAsync(userId, updateDto);

        // Assert
        await act.Should().ThrowAsync<BusinessValidationException>();
    }

    [Fact]
    public async Task DeleteAsync_WithExistingUser_DeletesSuccessfully()
    {
        // Arrange
        var userId = "user-123";
        var user = UserBuilder.CreateValid();

        _mockUserRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        // Act
        await _userService.DeleteAsync(userId);

        // Assert
        _mockUserRepository.Verify(r => r.DeleteAsync(userId), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistentUser_ThrowsEntityNotFoundException()
    {
        // Arrange
        var userId = "non-existent";
        _mockUserRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync((User?)null);

        // Act
        var act = async () => await _userService.DeleteAsync(userId);

        // Assert
        await act.Should().ThrowAsync<EntityNotFoundException>()
            .WithMessage("*User*not found*");
    }

    [Fact]
    public async Task GetStatsAsync_WithExistingUser_ReturnsUserStats()
    {
        // Arrange
        var userId = "user-123";
        var user = UserBuilder.CreateValid();
        var videos = new List<Video> { VideoBuilder.CreateValid(), VideoBuilder.CreateValid() };
        var jobs = new List<Job>
        {
            new() { Status = JobStatus.Completed },
            new() { Status = JobStatus.Completed },
            new() { Status = JobStatus.Failed }
        };

        _mockUserRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
        _mockVideoRepository
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Video, bool>>>()))
            .ReturnsAsync(videos);
        _mockJobRepository
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Job, bool>>>()))
            .ReturnsAsync(jobs);

        // Act
        var result = await _userService.GetStatsAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(userId);
        result.TotalVideos.Should().Be(2);
        result.TotalJobs.Should().Be(3);
        result.CompletedJobs.Should().Be(2);
        result.FailedJobs.Should().Be(1);
    }

    [Fact]
    public async Task ExistsAsync_WithExistingEmail_ReturnsTrue()
    {
        // Arrange
        var email = "exists@example.com";
        _mockUserRepository
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>()))
            .ReturnsAsync(new List<User> { UserBuilder.CreateValid() });

        // Act
        var result = await _userService.ExistsAsync(email);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_WithNonExistentEmail_ReturnsFalse()
    {
        // Arrange
        var email = "nonexistent@example.com";
        _mockUserRepository
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>()))
            .ReturnsAsync(new List<User>());

        // Act
        var result = await _userService.ExistsAsync(email);

        // Assert
        result.Should().BeFalse();
    }
}
