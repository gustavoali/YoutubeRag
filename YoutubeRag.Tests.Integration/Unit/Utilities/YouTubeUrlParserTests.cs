using FluentAssertions;
using Xunit;
using YoutubeRag.Application.Utilities;

namespace YoutubeRag.Tests.Integration.Unit.Utilities;

/// <summary>
/// Unit tests for YouTubeUrlParser utility class
/// Tests various YouTube URL formats and validation scenarios
/// </summary>
public class YouTubeUrlParserTests
{
    #region IsValidYouTubeUrl Tests

    [Theory]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("https://youtube.com/watch?v=dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("http://www.youtube.com/watch?v=dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("http://youtube.com/watch?v=dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("www.youtube.com/watch?v=dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("youtube.com/watch?v=dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    public void IsValidYouTubeUrl_WatchFormat_ShouldReturnTrueAndExtractVideoId(string url, string expectedVideoId)
    {
        // Act
        var result = YouTubeUrlParser.IsValidYouTubeUrl(url, out string? videoId);

        // Assert
        result.Should().BeTrue();
        videoId.Should().Be(expectedVideoId);
    }

    [Theory]
    [InlineData("https://youtu.be/dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("http://youtu.be/dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("youtu.be/dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    public void IsValidYouTubeUrl_ShortFormat_ShouldReturnTrueAndExtractVideoId(string url, string expectedVideoId)
    {
        // Act
        var result = YouTubeUrlParser.IsValidYouTubeUrl(url, out string? videoId);

        // Assert
        result.Should().BeTrue();
        videoId.Should().Be(expectedVideoId);
    }

    [Theory]
    [InlineData("https://www.youtube.com/embed/dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("https://youtube.com/embed/dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("youtube.com/embed/dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    public void IsValidYouTubeUrl_EmbedFormat_ShouldReturnTrueAndExtractVideoId(string url, string expectedVideoId)
    {
        // Act
        var result = YouTubeUrlParser.IsValidYouTubeUrl(url, out string? videoId);

        // Assert
        result.Should().BeTrue();
        videoId.Should().Be(expectedVideoId);
    }

    [Theory]
    [InlineData("https://www.youtube.com/v/dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("https://youtube.com/v/dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("youtube.com/v/dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    public void IsValidYouTubeUrl_VFormat_ShouldReturnTrueAndExtractVideoId(string url, string expectedVideoId)
    {
        // Act
        var result = YouTubeUrlParser.IsValidYouTubeUrl(url, out string? videoId);

        // Assert
        result.Should().BeTrue();
        videoId.Should().Be(expectedVideoId);
    }

    [Theory]
    [InlineData("https://www.youtube.com/shorts/dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("https://youtube.com/shorts/dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("youtube.com/shorts/dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    public void IsValidYouTubeUrl_ShortsFormat_ShouldReturnTrueAndExtractVideoId(string url, string expectedVideoId)
    {
        // Act
        var result = YouTubeUrlParser.IsValidYouTubeUrl(url, out string? videoId);

        // Assert
        result.Should().BeTrue();
        videoId.Should().Be(expectedVideoId);
    }

    [Theory]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ&feature=share")]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ&t=10s")]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ&list=PLrAXtmErZgOeiKm4sgNOknGvNjby9efdf")]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ&index=2&list=PLrAXtmErZgOeiKm4sgNOknGvNjby9efdf")]
    public void IsValidYouTubeUrl_WithQueryParameters_ShouldReturnTrueAndExtractVideoId(string url)
    {
        // Act
        var result = YouTubeUrlParser.IsValidYouTubeUrl(url, out string? videoId);

        // Assert
        result.Should().BeTrue();
        videoId.Should().Be("dQw4w9WgXcQ");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    public void IsValidYouTubeUrl_EmptyOrNull_ShouldReturnFalse(string? url)
    {
        // Act
        var result = YouTubeUrlParser.IsValidYouTubeUrl(url, out string? videoId);

        // Assert
        result.Should().BeFalse();
        videoId.Should().BeNull();
    }

    [Theory]
    [InlineData("https://www.google.com")]
    [InlineData("https://vimeo.com/123456789")]
    [InlineData("https://facebook.com/watch?v=123")]
    [InlineData("not a url")]
    [InlineData("youtube.com")]
    [InlineData("https://www.youtube.com")]
    public void IsValidYouTubeUrl_InvalidUrls_ShouldReturnFalse(string url)
    {
        // Act
        var result = YouTubeUrlParser.IsValidYouTubeUrl(url, out string? videoId);

        // Assert
        result.Should().BeFalse();
        videoId.Should().BeNullOrEmpty();
    }

    [Theory]
    [InlineData("https://www.youtube.com/watch?v=short")]      // Too short
    [InlineData("https://www.youtube.com/watch?v=invalid@id")]  // Invalid characters
    public void IsValidYouTubeUrl_InvalidVideoIdFormat_ShouldReturnFalse(string url)
    {
        // Act
        var result = YouTubeUrlParser.IsValidYouTubeUrl(url, out string? videoId);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region ExtractVideoId Tests

    [Theory]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("https://youtu.be/dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("https://www.youtube.com/embed/dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("https://www.youtube.com/v/dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("https://www.youtube.com/shorts/dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    public void ExtractVideoId_ValidUrls_ShouldReturnVideoId(string url, string expectedVideoId)
    {
        // Act
        var videoId = YouTubeUrlParser.ExtractVideoId(url);

        // Assert
        videoId.Should().Be(expectedVideoId);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("https://www.google.com")]
    [InlineData("invalid url")]
    public void ExtractVideoId_InvalidUrls_ShouldReturnNull(string? url)
    {
        // Act
        var videoId = YouTubeUrlParser.ExtractVideoId(url);

        // Assert
        videoId.Should().BeNull();
    }

    #endregion

    #region ValidateVideoId Tests

    [Theory]
    [InlineData("dQw4w9WgXcQ")]
    [InlineData("_-abcXYZ012")]
    [InlineData("12345678901")]
    public void ValidateVideoId_ValidId_ShouldReturnTrue(string videoId)
    {
        // Act
        var result = YouTubeUrlParser.ValidateVideoId(videoId);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    [InlineData("short")]         // Too short
    [InlineData("toolongvideoid")] // Too long
    [InlineData("invalid@char")]  // Invalid character
    [InlineData("has spaces!!")]  // Spaces
    public void ValidateVideoId_InvalidId_ShouldReturnFalse(string? videoId)
    {
        // Act
        var result = YouTubeUrlParser.ValidateVideoId(videoId);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region IsYouTubeUrl Tests

    [Theory]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ")]
    [InlineData("https://youtube.com/anything")]
    [InlineData("https://youtu.be/dQw4w9WgXcQ")]
    [InlineData("youtu.be/something")]
    [InlineData("www.youtube.com")]
    public void IsYouTubeUrl_YouTubeUrls_ShouldReturnTrue(string url)
    {
        // Act
        var result = YouTubeUrlParser.IsYouTubeUrl(url);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("https://www.google.com")]
    [InlineData("https://vimeo.com/123456")]
    [InlineData("not a url")]
    [InlineData("")]
    [InlineData(null)]
    public void IsYouTubeUrl_NonYouTubeUrls_ShouldReturnFalse(string? url)
    {
        // Act
        var result = YouTubeUrlParser.IsYouTubeUrl(url);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region NormalizeUrl Tests

    [Theory]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ", "https://www.youtube.com/watch?v=dQw4w9WgXcQ")]
    [InlineData("https://youtu.be/dQw4w9WgXcQ", "https://www.youtube.com/watch?v=dQw4w9WgXcQ")]
    [InlineData("https://www.youtube.com/embed/dQw4w9WgXcQ", "https://www.youtube.com/watch?v=dQw4w9WgXcQ")]
    [InlineData("https://www.youtube.com/v/dQw4w9WgXcQ", "https://www.youtube.com/watch?v=dQw4w9WgXcQ")]
    [InlineData("https://www.youtube.com/shorts/dQw4w9WgXcQ", "https://www.youtube.com/watch?v=dQw4w9WgXcQ")]
    public void NormalizeUrl_ValidUrls_ShouldReturnNormalizedUrl(string url, string expected)
    {
        // Act
        var normalized = YouTubeUrlParser.NormalizeUrl(url);

        // Assert
        normalized.Should().Be(expected);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("https://www.google.com")]
    [InlineData("invalid url")]
    public void NormalizeUrl_InvalidUrls_ShouldReturnNull(string? url)
    {
        // Act
        var normalized = YouTubeUrlParser.NormalizeUrl(url);

        // Assert
        normalized.Should().BeNull();
    }

    #endregion

    #region Validate Method Tests

    [Fact]
    public void Validate_ValidUrl_ShouldReturnSuccessfulResult()
    {
        // Arrange
        var url = "https://www.youtube.com/watch?v=dQw4w9WgXcQ";

        // Act
        var result = YouTubeUrlParser.Validate(url);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.VideoId.Should().Be("dQw4w9WgXcQ");
        result.ErrorMessage.Should().BeNull();
        result.NormalizedUrl.Should().Be("https://www.youtube.com/watch?v=dQw4w9WgXcQ");
    }

    [Fact]
    public void Validate_EmptyUrl_ShouldReturnErrorResult()
    {
        // Act
        var result = YouTubeUrlParser.Validate("");

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.VideoId.Should().BeNull();
        result.ErrorMessage.Should().Be("URL cannot be empty");
        result.NormalizedUrl.Should().BeNull();
    }

    [Fact]
    public void Validate_NonYouTubeUrl_ShouldReturnErrorResult()
    {
        // Arrange
        var url = "https://www.google.com";

        // Act
        var result = YouTubeUrlParser.Validate(url);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.VideoId.Should().BeNull();
        result.ErrorMessage.Should().Be("URL must be a valid YouTube URL (youtube.com or youtu.be)");
        result.NormalizedUrl.Should().BeNull();
    }

    [Fact]
    public void Validate_InvalidVideoId_ShouldReturnErrorResult()
    {
        // Arrange
        var url = "https://www.youtube.com/watch?v=short";

        // Act
        var result = YouTubeUrlParser.Validate(url);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.VideoId.Should().BeNull();
        result.ErrorMessage.Should().Be("Could not extract a valid YouTube video ID from the URL");
        result.NormalizedUrl.Should().BeNull();
    }

    #endregion

    #region Edge Cases and Special Scenarios

    [Fact]
    public void IsValidYouTubeUrl_UrlWithWhitespace_ShouldTrimAndValidate()
    {
        // Arrange
        var url = "  https://www.youtube.com/watch?v=dQw4w9WgXcQ  ";

        // Act
        var result = YouTubeUrlParser.IsValidYouTubeUrl(url, out string? videoId);

        // Assert
        result.Should().BeTrue();
        videoId.Should().Be("dQw4w9WgXcQ");
    }

    [Theory]
    [InlineData("https://www.youtube.com/watch?v=0123456789A")]
    [InlineData("https://www.youtube.com/watch?v=ABCDEFGHIJK")]
    [InlineData("https://www.youtube.com/watch?v=___________")]
    [InlineData("https://www.youtube.com/watch?v=-----------")]
    public void IsValidYouTubeUrl_EdgeCaseVideoIds_ShouldValidateCorrectly(string url)
    {
        // Act
        var result = YouTubeUrlParser.IsValidYouTubeUrl(url, out string? videoId);

        // Assert
        result.Should().BeTrue();
        videoId.Should().HaveLength(11);
    }

    #endregion
}
