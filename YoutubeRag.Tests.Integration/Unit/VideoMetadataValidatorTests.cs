using FluentAssertions;
using FluentValidation.TestHelper;
using Xunit;
using YoutubeRag.Application.DTOs.Video;
using YoutubeRag.Application.Validators.Video;

namespace YoutubeRag.Tests.Integration.Unit;

/// <summary>
/// Unit tests for VideoMetadataValidator (YRUS-0102 AC3)
/// Tests validation rules for video metadata
/// </summary>
public class VideoMetadataValidatorTests
{
    private readonly VideoMetadataValidator _validator;

    public VideoMetadataValidatorTests()
    {
        _validator = new VideoMetadataValidator();
    }

    #region Title Validation Tests

    [Fact]
    public async Task Validate_EmptyTitle_ShouldHaveValidationError()
    {
        // Arrange
        var metadata = CreateValidMetadata();
        metadata.Title = string.Empty;

        // Act
        var result = await _validator.TestValidateAsync(metadata);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Title)
            .WithErrorCode("INVALID_TITLE");
    }

    [Fact]
    public async Task Validate_NullTitle_ShouldHaveValidationError()
    {
        // Arrange
        var metadata = CreateValidMetadata();
        metadata.Title = null!;

        // Act
        var result = await _validator.TestValidateAsync(metadata);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Title)
            .WithErrorCode("INVALID_TITLE");
    }

    [Fact]
    public async Task Validate_ValidTitle_ShouldNotHaveValidationError()
    {
        // Arrange
        var metadata = CreateValidMetadata();
        metadata.Title = "Valid Video Title";

        // Act
        var result = await _validator.TestValidateAsync(metadata);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public async Task Validate_TitleTooLong_ShouldHaveValidationError()
    {
        // Arrange
        var metadata = CreateValidMetadata();
        metadata.Title = new string('A', 501); // 501 characters

        // Act
        var result = await _validator.TestValidateAsync(metadata);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Title)
            .WithErrorCode("TITLE_TOO_LONG");
    }

    #endregion

    #region Duration Validation Tests

    [Fact]
    public async Task Validate_NullDuration_ShouldHaveValidationError()
    {
        // Arrange
        var metadata = CreateValidMetadata();
        metadata.Duration = null;

        // Act
        var result = await _validator.TestValidateAsync(metadata);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Duration)
            .WithErrorCode("INVALID_DURATION");
    }

    [Fact]
    public async Task Validate_ZeroDuration_ShouldHaveValidationError()
    {
        // Arrange
        var metadata = CreateValidMetadata();
        metadata.Duration = TimeSpan.Zero;

        // Act
        var result = await _validator.TestValidateAsync(metadata);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.DurationSeconds)
            .WithErrorCode("INVALID_DURATION");
    }

    [Fact]
    public async Task Validate_DurationTooLong_ShouldHaveValidationError()
    {
        // Arrange - YRUS-0102 AC3: Max 4 hours (14400 seconds)
        var metadata = CreateValidMetadata();
        metadata.Duration = TimeSpan.FromSeconds(14401); // 4 hours + 1 second

        // Act
        var result = await _validator.TestValidateAsync(metadata);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.DurationSeconds)
            .WithErrorCode("VIDEO_TOO_LONG");
    }

    [Fact]
    public async Task Validate_ValidDuration_ShouldNotHaveValidationError()
    {
        // Arrange
        var metadata = CreateValidMetadata();
        metadata.Duration = TimeSpan.FromMinutes(30); // 30 minutes

        // Act
        var result = await _validator.TestValidateAsync(metadata);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Duration);
        result.ShouldNotHaveValidationErrorFor(x => x.DurationSeconds);
    }

    [Fact]
    public async Task Validate_MaxDuration_ShouldNotHaveValidationError()
    {
        // Arrange
        var metadata = CreateValidMetadata();
        metadata.Duration = TimeSpan.FromSeconds(14400); // Exactly 4 hours

        // Act
        var result = await _validator.TestValidateAsync(metadata);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.DurationSeconds);
    }

    #endregion

    #region Thumbnail URL Validation Tests

    [Fact]
    public async Task Validate_EmptyThumbnailUrl_ShouldHaveValidationError()
    {
        // Arrange
        var metadata = CreateValidMetadata();
        metadata.ThumbnailUrls.Clear();

        // Act
        var result = await _validator.TestValidateAsync(metadata);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ThumbnailUrl)
            .WithErrorCode("INVALID_THUMBNAIL");
    }

    [Fact]
    public async Task Validate_InvalidThumbnailUrl_ShouldHaveValidationError()
    {
        // Arrange
        var metadata = CreateValidMetadata();
        metadata.ThumbnailUrls.Clear();
        metadata.ThumbnailUrls.Add("not-a-valid-url");

        // Act
        var result = await _validator.TestValidateAsync(metadata);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ThumbnailUrl)
            .WithErrorCode("INVALID_THUMBNAIL_URL");
    }

    [Fact]
    public async Task Validate_ValidThumbnailUrl_ShouldNotHaveValidationError()
    {
        // Arrange
        var metadata = CreateValidMetadata();
        metadata.ThumbnailUrls.Clear();
        metadata.ThumbnailUrls.Add("https://i.ytimg.com/vi/dQw4w9WgXcQ/maxresdefault.jpg");

        // Act
        var result = await _validator.TestValidateAsync(metadata);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ThumbnailUrl);
    }

    #endregion

    #region Channel Validation Tests

    [Fact]
    public async Task Validate_EmptyChannelTitle_ShouldHaveValidationError()
    {
        // Arrange
        var metadata = CreateValidMetadata();
        metadata.ChannelTitle = string.Empty;

        // Act
        var result = await _validator.TestValidateAsync(metadata);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ChannelTitle)
            .WithErrorCode("INVALID_CHANNEL");
    }

    [Fact]
    public async Task Validate_ValidChannelTitle_ShouldNotHaveValidationError()
    {
        // Arrange
        var metadata = CreateValidMetadata();
        metadata.ChannelTitle = "Valid Channel Name";

        // Act
        var result = await _validator.TestValidateAsync(metadata);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ChannelTitle);
    }

    #endregion

    #region Published Date Validation Tests

    [Fact]
    public async Task Validate_NullPublishedAt_ShouldHaveValidationError()
    {
        // Arrange
        var metadata = CreateValidMetadata();
        metadata.PublishedAt = null;

        // Act
        var result = await _validator.TestValidateAsync(metadata);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PublishedAt)
            .WithErrorCode("INVALID_PUBLISHED_DATE");
    }

    [Fact]
    public async Task Validate_FuturePublishedAt_ShouldHaveValidationError()
    {
        // Arrange
        var metadata = CreateValidMetadata();
        metadata.PublishedAt = DateTime.UtcNow.AddDays(2); // 2 days in future

        // Act
        var result = await _validator.TestValidateAsync(metadata);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PublishedAt)
            .WithErrorCode("INVALID_PUBLISHED_DATE");
    }

    [Fact]
    public async Task Validate_ValidPublishedAt_ShouldNotHaveValidationError()
    {
        // Arrange
        var metadata = CreateValidMetadata();
        metadata.PublishedAt = DateTime.UtcNow.AddDays(-30); // 30 days ago

        // Act
        var result = await _validator.TestValidateAsync(metadata);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.PublishedAt);
    }

    #endregion

    #region Complete Metadata Validation Tests

    [Fact]
    public async Task Validate_CompleteValidMetadata_ShouldPassAllValidations()
    {
        // Arrange
        var metadata = CreateValidMetadata();

        // Act
        var result = await _validator.TestValidateAsync(metadata);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task Validate_MultipleErrors_ShouldReturnAllErrors()
    {
        // Arrange
        var metadata = new VideoMetadataDto
        {
            Title = string.Empty,
            Duration = null,
            ThumbnailUrls = new List<string>(),
            ChannelTitle = string.Empty,
            PublishedAt = null
        };

        // Act
        var result = await _validator.TestValidateAsync(metadata);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Count.Should().BeGreaterThanOrEqualTo(4);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Creates a valid VideoMetadataDto for testing
    /// </summary>
    private VideoMetadataDto CreateValidMetadata()
    {
        return new VideoMetadataDto
        {
            Title = "Valid Video Title",
            Description = "Valid video description",
            Duration = TimeSpan.FromMinutes(10),
            ViewCount = 1000,
            LikeCount = 50,
            PublishedAt = DateTime.UtcNow.AddDays(-7),
            ChannelId = "UCxxxxxxxxxxxxxxxxxxxxxx",
            ChannelTitle = "Valid Channel Name",
            ThumbnailUrls = new List<string>
            {
                "https://i.ytimg.com/vi/dQw4w9WgXcQ/maxresdefault.jpg"
            },
            Tags = new List<string> { "tag1", "tag2" },
            CategoryId = "10"
        };
    }

    #endregion
}
