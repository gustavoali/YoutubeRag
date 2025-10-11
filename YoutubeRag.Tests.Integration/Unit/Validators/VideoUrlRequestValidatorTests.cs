using FluentAssertions;
using FluentValidation.TestHelper;
using Xunit;
using YoutubeRag.Application.DTOs.Video;
using YoutubeRag.Application.Validators.Video;

namespace YoutubeRag.Tests.Integration.Unit.Validators;

/// <summary>
/// Unit tests for VideoUrlRequestValidator
/// Tests validation rules for VideoUrlRequest DTOs
/// </summary>
public class VideoUrlRequestValidatorTests
{
    private readonly VideoUrlRequestValidator _validator;

    public VideoUrlRequestValidatorTests()
    {
        _validator = new VideoUrlRequestValidator();
    }

    #region URL Validation Tests

    [Theory]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ")]
    [InlineData("https://youtu.be/dQw4w9WgXcQ")]
    [InlineData("https://www.youtube.com/embed/dQw4w9WgXcQ")]
    [InlineData("https://www.youtube.com/v/dQw4w9WgXcQ")]
    [InlineData("https://www.youtube.com/shorts/dQw4w9WgXcQ")]
    public void Validate_ValidYouTubeUrls_ShouldPass(string url)
    {
        // Arrange
        var request = new VideoUrlRequest
        {
            Url = url,
            Priority = ProcessingPriority.Normal
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyUrl_ShouldHaveValidationError()
    {
        // Arrange
        var request = new VideoUrlRequest
        {
            Url = string.Empty,
            Priority = ProcessingPriority.Normal
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Url)
            .WithErrorMessage("URL is required")
            .WithErrorCode("URL_REQUIRED");
    }

    [Fact]
    public void Validate_NullUrl_ShouldHaveValidationError()
    {
        // Arrange
        var request = new VideoUrlRequest
        {
            Url = null!,
            Priority = ProcessingPriority.Normal
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Url)
            .WithErrorMessage("URL is required")
            .WithErrorCode("URL_REQUIRED");
    }

    [Theory]
    [InlineData("https://www.google.com")]
    [InlineData("https://vimeo.com/123456789")]
    [InlineData("https://facebook.com/watch?v=123")]
    [InlineData("not a url")]
    public void Validate_NonYouTubeUrl_ShouldHaveValidationError(string url)
    {
        // Arrange
        var request = new VideoUrlRequest
        {
            Url = url,
            Priority = ProcessingPriority.Normal
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Url)
            .WithErrorCode("INVALID_YOUTUBE_URL");
    }

    [Theory]
    [InlineData("https://www.youtube.com/watch?v=short")]
    [InlineData("https://www.youtube.com/watch?v=invalid@id")]
    public void Validate_InvalidVideoIdFormat_ShouldHaveValidationError(string url)
    {
        // Arrange
        var request = new VideoUrlRequest
        {
            Url = url,
            Priority = ProcessingPriority.Normal
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Url)
            .WithErrorCode("INVALID_VIDEO_ID");
    }

    [Fact]
    public void Validate_UrlTooLong_ShouldHaveValidationError()
    {
        // Arrange
        var longUrl = "https://www.youtube.com/watch?v=dQw4w9WgXcQ&" + new string('a', 2100);
        var request = new VideoUrlRequest
        {
            Url = longUrl,
            Priority = ProcessingPriority.Normal
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Url)
            .WithErrorMessage("URL cannot exceed 2048 characters")
            .WithErrorCode("URL_TOO_LONG");
    }

    #endregion

    #region Title Validation Tests

    [Fact]
    public void Validate_NullTitle_ShouldPass()
    {
        // Arrange
        var request = new VideoUrlRequest
        {
            Url = "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
            Title = null,
            Priority = ProcessingPriority.Normal
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public void Validate_ValidTitle_ShouldPass()
    {
        // Arrange
        var request = new VideoUrlRequest
        {
            Url = "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
            Title = "My Video Title",
            Priority = ProcessingPriority.Normal
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public void Validate_TitleTooLong_ShouldHaveValidationError()
    {
        // Arrange
        var request = new VideoUrlRequest
        {
            Url = "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
            Title = new string('a', 256),
            Priority = ProcessingPriority.Normal
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Title)
            .WithErrorMessage("Title cannot exceed 255 characters")
            .WithErrorCode("TITLE_TOO_LONG");
    }

    [Fact]
    public void Validate_TitleExactly255Characters_ShouldPass()
    {
        // Arrange
        var request = new VideoUrlRequest
        {
            Url = "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
            Title = new string('a', 255),
            Priority = ProcessingPriority.Normal
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Title);
    }

    #endregion

    #region Description Validation Tests

    [Fact]
    public void Validate_NullDescription_ShouldPass()
    {
        // Arrange
        var request = new VideoUrlRequest
        {
            Url = "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
            Description = null,
            Priority = ProcessingPriority.Normal
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void Validate_ValidDescription_ShouldPass()
    {
        // Arrange
        var request = new VideoUrlRequest
        {
            Url = "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
            Description = "This is a valid description",
            Priority = ProcessingPriority.Normal
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void Validate_DescriptionTooLong_ShouldHaveValidationError()
    {
        // Arrange
        var request = new VideoUrlRequest
        {
            Url = "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
            Description = new string('a', 5001),
            Priority = ProcessingPriority.Normal
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Description)
            .WithErrorMessage("Description cannot exceed 5000 characters")
            .WithErrorCode("DESCRIPTION_TOO_LONG");
    }

    [Fact]
    public void Validate_DescriptionExactly5000Characters_ShouldPass()
    {
        // Arrange
        var request = new VideoUrlRequest
        {
            Url = "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
            Description = new string('a', 5000),
            Priority = ProcessingPriority.Normal
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Description);
    }

    #endregion

    #region Priority Validation Tests

    [Theory]
    [InlineData(ProcessingPriority.Low)]
    [InlineData(ProcessingPriority.Normal)]
    [InlineData(ProcessingPriority.High)]
    [InlineData(ProcessingPriority.Critical)]
    public void Validate_ValidPriority_ShouldPass(ProcessingPriority priority)
    {
        // Arrange
        var request = new VideoUrlRequest
        {
            Url = "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
            Priority = priority
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Priority);
    }

    [Fact]
    public void Validate_InvalidPriority_ShouldHaveValidationError()
    {
        // Arrange
        var request = new VideoUrlRequest
        {
            Url = "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
            Priority = (ProcessingPriority)999
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Priority)
            .WithErrorCode("INVALID_PRIORITY");
    }

    #endregion

    #region Complex Validation Scenarios

    [Fact]
    public void Validate_CompleteValidRequest_ShouldPass()
    {
        // Arrange
        var request = new VideoUrlRequest
        {
            Url = "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
            Title = "Complete Video Title",
            Description = "This is a complete and valid description",
            Priority = ProcessingPriority.High
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_MinimalValidRequest_ShouldPass()
    {
        // Arrange
        var request = new VideoUrlRequest
        {
            Url = "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
            Priority = ProcessingPriority.Normal
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_MultipleErrors_ShouldReturnAllErrors()
    {
        // Arrange
        var request = new VideoUrlRequest
        {
            Url = "",
            Title = new string('a', 300),
            Description = new string('b', 5500),
            Priority = (ProcessingPriority)999
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Url);
        result.ShouldHaveValidationErrorFor(x => x.Title);
        result.ShouldHaveValidationErrorFor(x => x.Description);
        result.ShouldHaveValidationErrorFor(x => x.Priority);
    }

    #endregion

    #region Real-World Scenarios

    [Theory]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ&feature=share")]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ&t=42s")]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ&list=PLxxxx&index=5")]
    public void Validate_UrlWithCommonParameters_ShouldPass(string url)
    {
        // Arrange
        var request = new VideoUrlRequest
        {
            Url = url,
            Priority = ProcessingPriority.Normal
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_UrlWithWhitespace_ShouldPass()
    {
        // Arrange
        var request = new VideoUrlRequest
        {
            Url = "  https://www.youtube.com/watch?v=dQw4w9WgXcQ  ",
            Priority = ProcessingPriority.Normal
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion
}
