using FluentAssertions;
using YoutubeRag.Application.Utilities;

namespace YoutubeRag.Tests.Unit.Application.Utilities;

/// <summary>
/// Unit tests for YouTubeUrlParser utility class.
/// Tests URL validation, video ID extraction, and normalization logic.
/// </summary>
public class YouTubeUrlParserTests
{
    #region IsValidYouTubeUrl Tests

    [Fact]
    public void IsValidYouTubeUrl_WithNullUrl_ReturnsFalse()
    {
        // Act
        var result = YouTubeUrlParser.IsValidYouTubeUrl(null, out string? videoId);

        // Assert
        result.Should().BeFalse();
        videoId.Should().BeNull();
    }

    [Fact]
    public void IsValidYouTubeUrl_WithEmptyString_ReturnsFalse()
    {
        // Act
        var result = YouTubeUrlParser.IsValidYouTubeUrl("", out string? videoId);

        // Assert
        result.Should().BeFalse();
        videoId.Should().BeNull();
    }

    [Fact]
    public void IsValidYouTubeUrl_WithWhitespace_ReturnsFalse()
    {
        // Act
        var result = YouTubeUrlParser.IsValidYouTubeUrl("   ", out string? videoId);

        // Assert
        result.Should().BeFalse();
        videoId.Should().BeNull();
    }

    [Theory]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ")]
    [InlineData("http://www.youtube.com/watch?v=dQw4w9WgXcQ")]
    [InlineData("https://youtube.com/watch?v=dQw4w9WgXcQ")]
    [InlineData("www.youtube.com/watch?v=dQw4w9WgXcQ")]
    [InlineData("youtube.com/watch?v=dQw4w9WgXcQ")]
    public void IsValidYouTubeUrl_WithValidWatchUrl_ReturnsTrue(string url)
    {
        // Act
        var result = YouTubeUrlParser.IsValidYouTubeUrl(url, out string? videoId);

        // Assert
        result.Should().BeTrue();
        videoId.Should().Be("dQw4w9WgXcQ");
    }

    [Theory]
    [InlineData("https://youtu.be/dQw4w9WgXcQ")]
    [InlineData("http://youtu.be/dQw4w9WgXcQ")]
    [InlineData("youtu.be/dQw4w9WgXcQ")]
    public void IsValidYouTubeUrl_WithValidShortUrl_ReturnsTrue(string url)
    {
        // Act
        var result = YouTubeUrlParser.IsValidYouTubeUrl(url, out string? videoId);

        // Assert
        result.Should().BeTrue();
        videoId.Should().Be("dQw4w9WgXcQ");
    }

    [Theory]
    [InlineData("https://www.youtube.com/embed/dQw4w9WgXcQ")]
    [InlineData("https://youtube.com/embed/dQw4w9WgXcQ")]
    public void IsValidYouTubeUrl_WithValidEmbedUrl_ReturnsTrue(string url)
    {
        // Act
        var result = YouTubeUrlParser.IsValidYouTubeUrl(url, out string? videoId);

        // Assert
        result.Should().BeTrue();
        videoId.Should().Be("dQw4w9WgXcQ");
    }

    [Theory]
    [InlineData("https://www.youtube.com/v/dQw4w9WgXcQ")]
    [InlineData("https://youtube.com/v/dQw4w9WgXcQ")]
    public void IsValidYouTubeUrl_WithValidVUrl_ReturnsTrue(string url)
    {
        // Act
        var result = YouTubeUrlParser.IsValidYouTubeUrl(url, out string? videoId);

        // Assert
        result.Should().BeTrue();
        videoId.Should().Be("dQw4w9WgXcQ");
    }

    [Theory]
    [InlineData("https://www.youtube.com/shorts/dQw4w9WgXcQ")]
    [InlineData("https://youtube.com/shorts/dQw4w9WgXcQ")]
    public void IsValidYouTubeUrl_WithValidShortsUrl_ReturnsTrue(string url)
    {
        // Act
        var result = YouTubeUrlParser.IsValidYouTubeUrl(url, out string? videoId);

        // Assert
        result.Should().BeTrue();
        videoId.Should().Be("dQw4w9WgXcQ");
    }

    [Fact]
    public void IsValidYouTubeUrl_WithQueryParameters_ExtractsVideoId()
    {
        // Arrange
        var url = "https://www.youtube.com/watch?v=dQw4w9WgXcQ&t=10s&list=PLTest";

        // Act
        var result = YouTubeUrlParser.IsValidYouTubeUrl(url, out string? videoId);

        // Assert
        result.Should().BeTrue();
        videoId.Should().Be("dQw4w9WgXcQ");
    }

    [Fact]
    public void IsValidYouTubeUrl_WithInvalidVideoId_ReturnsFalse()
    {
        // Arrange - video ID too short
        var url = "https://www.youtube.com/watch?v=short";

        // Act
        var result = YouTubeUrlParser.IsValidYouTubeUrl(url, out string? videoId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsValidYouTubeUrl_WithCompletelyInvalidUrl_ReturnsFalse()
    {
        // Arrange
        var url = "https://www.example.com/video/123";

        // Act
        var result = YouTubeUrlParser.IsValidYouTubeUrl(url, out string? videoId);

        // Assert
        result.Should().BeFalse();
        videoId.Should().BeNull();
    }

    [Fact]
    public void IsValidYouTubeUrl_WithUrlAndExtraWhitespace_TrimsAndParsesCorrectly()
    {
        // Arrange
        var url = "  https://www.youtube.com/watch?v=dQw4w9WgXcQ  ";

        // Act
        var result = YouTubeUrlParser.IsValidYouTubeUrl(url, out string? videoId);

        // Assert
        result.Should().BeTrue();
        videoId.Should().Be("dQw4w9WgXcQ");
    }

    #endregion

    #region ExtractVideoId Tests

    [Fact]
    public void ExtractVideoId_WithValidUrl_ReturnsVideoId()
    {
        // Arrange
        var url = "https://www.youtube.com/watch?v=dQw4w9WgXcQ";

        // Act
        var videoId = YouTubeUrlParser.ExtractVideoId(url);

        // Assert
        videoId.Should().Be("dQw4w9WgXcQ");
    }

    [Fact]
    public void ExtractVideoId_WithInvalidUrl_ReturnsNull()
    {
        // Arrange
        var url = "https://www.example.com/video";

        // Act
        var videoId = YouTubeUrlParser.ExtractVideoId(url);

        // Assert
        videoId.Should().BeNull();
    }

    [Fact]
    public void ExtractVideoId_WithNullUrl_ReturnsNull()
    {
        // Act
        var videoId = YouTubeUrlParser.ExtractVideoId(null);

        // Assert
        videoId.Should().BeNull();
    }

    #endregion

    #region ValidateVideoId Tests

    [Fact]
    public void ValidateVideoId_WithNullId_ReturnsFalse()
    {
        // Act
        var result = YouTubeUrlParser.ValidateVideoId(null);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateVideoId_WithEmptyString_ReturnsFalse()
    {
        // Act
        var result = YouTubeUrlParser.ValidateVideoId("");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateVideoId_WithWhitespace_ReturnsFalse()
    {
        // Act
        var result = YouTubeUrlParser.ValidateVideoId("   ");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateVideoId_WithValid11CharId_ReturnsTrue()
    {
        // Arrange
        var videoId = "dQw4w9WgXcQ";

        // Act
        var result = YouTubeUrlParser.ValidateVideoId(videoId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateVideoId_WithTooShortId_ReturnsFalse()
    {
        // Arrange
        var videoId = "short";

        // Act
        var result = YouTubeUrlParser.ValidateVideoId(videoId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateVideoId_WithTooLongId_ReturnsFalse()
    {
        // Arrange
        var videoId = "dQw4w9WgXcQextrachars";

        // Act
        var result = YouTubeUrlParser.ValidateVideoId(videoId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateVideoId_WithInvalidCharacters_ReturnsFalse()
    {
        // Arrange
        var videoId = "dQw4w9WgXc!";

        // Act
        var result = YouTubeUrlParser.ValidateVideoId(videoId);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("abcdefghijk")]
    [InlineData("ABCDEFGHIJK")]
    [InlineData("0123456789a")]
    [InlineData("abc-def_hij")]
    [InlineData("___________")]
    [InlineData("-----------")]
    public void ValidateVideoId_WithValidFormats_ReturnsTrue(string videoId)
    {
        // Act
        var result = YouTubeUrlParser.ValidateVideoId(videoId);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region IsYouTubeUrl Tests

    [Fact]
    public void IsYouTubeUrl_WithYouTubeDotCom_ReturnsTrue()
    {
        // Arrange
        var url = "https://www.youtube.com/anything";

        // Act
        var result = YouTubeUrlParser.IsYouTubeUrl(url);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsYouTubeUrl_WithYouTuDotBe_ReturnsTrue()
    {
        // Arrange
        var url = "https://youtu.be/anything";

        // Act
        var result = YouTubeUrlParser.IsYouTubeUrl(url);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsYouTubeUrl_WithNonYouTubeUrl_ReturnsFalse()
    {
        // Arrange
        var url = "https://www.vimeo.com/video";

        // Act
        var result = YouTubeUrlParser.IsYouTubeUrl(url);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsYouTubeUrl_WithNullUrl_ReturnsFalse()
    {
        // Act
        var result = YouTubeUrlParser.IsYouTubeUrl(null);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsYouTubeUrl_WithEmptyString_ReturnsFalse()
    {
        // Act
        var result = YouTubeUrlParser.IsYouTubeUrl("");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsYouTubeUrl_IsCaseInsensitive()
    {
        // Arrange
        var url = "https://www.YOUTUBE.COM/watch";

        // Act
        var result = YouTubeUrlParser.IsYouTubeUrl(url);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region NormalizeUrl Tests

    [Fact]
    public void NormalizeUrl_WithValidUrl_ReturnsNormalizedFormat()
    {
        // Arrange
        var url = "https://youtu.be/dQw4w9WgXcQ";

        // Act
        var normalized = YouTubeUrlParser.NormalizeUrl(url);

        // Assert
        normalized.Should().Be("https://www.youtube.com/watch?v=dQw4w9WgXcQ");
    }

    [Fact]
    public void NormalizeUrl_WithAlreadyNormalizedUrl_ReturnsNormalizedFormat()
    {
        // Arrange
        var url = "https://www.youtube.com/watch?v=dQw4w9WgXcQ";

        // Act
        var normalized = YouTubeUrlParser.NormalizeUrl(url);

        // Assert
        normalized.Should().Be("https://www.youtube.com/watch?v=dQw4w9WgXcQ");
    }

    [Fact]
    public void NormalizeUrl_WithInvalidUrl_ReturnsNull()
    {
        // Arrange
        var url = "https://www.example.com/video";

        // Act
        var normalized = YouTubeUrlParser.NormalizeUrl(url);

        // Assert
        normalized.Should().BeNull();
    }

    [Fact]
    public void NormalizeUrl_WithNullUrl_ReturnsNull()
    {
        // Act
        var normalized = YouTubeUrlParser.NormalizeUrl(null);

        // Assert
        normalized.Should().BeNull();
    }

    #endregion

    #region Validate Tests

    [Fact]
    public void Validate_WithNullUrl_ReturnsInvalidWithErrorMessage()
    {
        // Act
        var result = YouTubeUrlParser.Validate(null);

        // Assert
        result.IsValid.Should().BeFalse();
        result.VideoId.Should().BeNull();
        result.ErrorMessage.Should().Be("URL cannot be empty");
        result.NormalizedUrl.Should().BeNull();
    }

    [Fact]
    public void Validate_WithEmptyUrl_ReturnsInvalidWithErrorMessage()
    {
        // Act
        var result = YouTubeUrlParser.Validate("");

        // Assert
        result.IsValid.Should().BeFalse();
        result.VideoId.Should().BeNull();
        result.ErrorMessage.Should().Be("URL cannot be empty");
    }

    [Fact]
    public void Validate_WithNonYouTubeUrl_ReturnsInvalidWithErrorMessage()
    {
        // Arrange
        var url = "https://www.vimeo.com/video";

        // Act
        var result = YouTubeUrlParser.Validate(url);

        // Assert
        result.IsValid.Should().BeFalse();
        result.VideoId.Should().BeNull();
        result.ErrorMessage.Should().Be("URL must be a valid YouTube URL (youtube.com or youtu.be)");
    }

    [Fact]
    public void Validate_WithInvalidYouTubeUrl_ReturnsInvalidWithErrorMessage()
    {
        // Arrange
        var url = "https://www.youtube.com/invalid";

        // Act
        var result = YouTubeUrlParser.Validate(url);

        // Assert
        result.IsValid.Should().BeFalse();
        result.VideoId.Should().BeNull();
        result.ErrorMessage.Should().Be("Could not extract a valid YouTube video ID from the URL");
    }

    [Fact]
    public void Validate_WithValidUrl_ReturnsValidResult()
    {
        // Arrange
        var url = "https://www.youtube.com/watch?v=dQw4w9WgXcQ";

        // Act
        var result = YouTubeUrlParser.Validate(url);

        // Assert
        result.IsValid.Should().BeTrue();
        result.VideoId.Should().Be("dQw4w9WgXcQ");
        result.ErrorMessage.Should().BeNull();
        result.NormalizedUrl.Should().Be("https://www.youtube.com/watch?v=dQw4w9WgXcQ");
    }

    [Fact]
    public void Validate_WithValidShortUrl_ReturnsValidResultWithNormalizedUrl()
    {
        // Arrange
        var url = "https://youtu.be/dQw4w9WgXcQ";

        // Act
        var result = YouTubeUrlParser.Validate(url);

        // Assert
        result.IsValid.Should().BeTrue();
        result.VideoId.Should().Be("dQw4w9WgXcQ");
        result.ErrorMessage.Should().BeNull();
        result.NormalizedUrl.Should().Be("https://www.youtube.com/watch?v=dQw4w9WgXcQ");
    }

    #endregion

    #region YouTubeUrlValidationResult Tests

    [Fact]
    public void YouTubeUrlValidationResult_CanBeCreated()
    {
        // Act
        var result = new YouTubeUrlValidationResult
        {
            IsValid = true,
            VideoId = "dQw4w9WgXcQ",
            ErrorMessage = null,
            NormalizedUrl = "https://www.youtube.com/watch?v=dQw4w9WgXcQ"
        };

        // Assert
        result.IsValid.Should().BeTrue();
        result.VideoId.Should().Be("dQw4w9WgXcQ");
        result.ErrorMessage.Should().BeNull();
        result.NormalizedUrl.Should().NotBeNull();
    }

    #endregion
}
