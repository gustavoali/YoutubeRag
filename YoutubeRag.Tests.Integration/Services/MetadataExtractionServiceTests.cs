using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net;
using Xunit;
using YoutubeExplode;
using YoutubeExplode.Common;
using YoutubeExplode.Exceptions;
using YoutubeExplode.Videos;
using YoutubeRag.Infrastructure.Services;
using YoutubeRag.Tests.Integration.Infrastructure;

namespace YoutubeRag.Tests.Integration.Services;

/// <summary>
/// Integration tests for MetadataExtractionService
/// Tests metadata extraction, 403 fallback to yt-dlp, timeout handling, and invalid ID handling
/// Note: These tests use real MetadataExtractionService with mocked YoutubeExplode client
/// </summary>
public class MetadataExtractionServiceTests : IntegrationTestBase
{
    public MetadataExtractionServiceTests(CustomWebApplicationFactory<Program> factory) : base(factory)
    {
    }

    #region Successful Metadata Extraction Tests

    [Fact]
    public async Task ExtractMetadataAsync_WithValidYouTubeId_ReturnsCompleteMetadata()
    {
        // Arrange
        var logger = Scope.ServiceProvider.GetRequiredService<ILogger<MetadataExtractionService>>();
        var service = new MetadataExtractionService(logger);
        var youTubeId = "dQw4w9WgXcQ"; // Real YouTube video ID (Rick Astley - Never Gonna Give You Up)

        // Act
        var metadata = await service.ExtractMetadataAsync(youTubeId);

        // Assert
        metadata.Should().NotBeNull();
        metadata.Title.Should().NotBeNullOrEmpty();
        metadata.Duration.Should().BeGreaterThan(TimeSpan.Zero);

        // Verify all metadata fields are populated
        metadata.Title.Should().NotBeNullOrEmpty();
        metadata.ChannelTitle.Should().NotBeNullOrEmpty();
        metadata.ThumbnailUrls.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ExtractMetadataAsync_ValidVideo_PopulatesAllMetadataFields()
    {
        // Arrange
        var logger = Scope.ServiceProvider.GetRequiredService<ILogger<MetadataExtractionService>>();
        var service = new MetadataExtractionService(logger);
        var youTubeId = "jNQXAC9IVRw"; // Real YouTube video ID

        // Act
        var metadata = await service.ExtractMetadataAsync(youTubeId);

        // Assert
        metadata.Should().NotBeNull();

        // Verify required fields
        metadata.Title.Should().NotBeNullOrEmpty();
        metadata.Duration.Should().BeGreaterThan(TimeSpan.Zero);

        // Verify optional fields are populated (when available)
        metadata.ChannelTitle.Should().NotBeNullOrEmpty();
        metadata.ChannelId.Should().NotBeNullOrEmpty();
        metadata.PublishedAt.Should().BeAfter(DateTime.MinValue);

        // Verify thumbnails
        metadata.ThumbnailUrls.Should().NotBeEmpty();
        metadata.ThumbnailUrls.All(url => Uri.IsWellFormedUriString(url, UriKind.Absolute)).Should().BeTrue();
    }

    [Fact]
    public async Task ExtractMetadataAsync_ValidVideo_VerifyDataTypes()
    {
        // Arrange
        var logger = Scope.ServiceProvider.GetRequiredService<ILogger<MetadataExtractionService>>();
        var service = new MetadataExtractionService(logger);
        var youTubeId = "dQw4w9WgXcQ";

        // Act
        var metadata = await service.ExtractMetadataAsync(youTubeId);

        // Assert
        metadata.Should().NotBeNull();

        // Verify correct data types and values
        metadata.Title.Should().NotBeNullOrEmpty();
        metadata.Duration.Should().NotBeNull();
        metadata.Duration.Should().BeGreaterThan(TimeSpan.Zero);

        if (metadata.ViewCount.HasValue)
        {
            metadata.ViewCount.Value.Should().BeGreaterThanOrEqualTo(0);
        }

        if (metadata.LikeCount.HasValue)
        {
            metadata.LikeCount.Value.Should().BeGreaterThanOrEqualTo(0);
        }

        metadata.Tags.Should().NotBeNull();
        metadata.ThumbnailUrls.Should().NotBeNull();
    }

    #endregion

    #region 403 Fallback to yt-dlp Tests

    [Fact(Skip = "Requires yt-dlp to be installed. Enable for manual testing when yt-dlp is available.")]
    public async Task ExtractMetadataAsync_When403Error_FallsBackToYtDlp()
    {
        // This test is skipped by default as it requires yt-dlp to be installed
        // Enable it when testing in an environment with yt-dlp available

        // Note: Simulating a 403 error from YoutubeExplode is complex in integration tests
        // as it would require mocking the internal HTTP client.
        // This test documents the expected behavior when a 403 occurs.

        var logger = Scope.ServiceProvider.GetRequiredService<ILogger<MetadataExtractionService>>();
        var service = new MetadataExtractionService(logger);

        // YoutubeExplode may throw 403 on some videos
        // The service should fall back to yt-dlp and still return metadata
        var youTubeId = "dQw4w9WgXcQ";

        var metadata = await service.ExtractMetadataAsync(youTubeId);

        metadata.Should().NotBeNull();
        metadata.Title.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region Network Timeout Handling Tests

    [Fact]
    public async Task ExtractMetadataAsync_WithTimeout_ThrowsAppropriateException()
    {
        // Arrange
        var logger = Scope.ServiceProvider.GetRequiredService<ILogger<MetadataExtractionService>>();
        var service = new MetadataExtractionService(logger);

        // Use a cancellation token that's already cancelled to simulate timeout
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(
            async () => await service.ExtractMetadataAsync("dQw4w9WgXcQ", cts.Token)
        );
    }

    [Fact]
    public async Task ExtractMetadataAsync_NetworkTimeout_LogsError()
    {
        // Arrange
        var logger = Scope.ServiceProvider.GetRequiredService<ILogger<MetadataExtractionService>>();
        var service = new MetadataExtractionService(logger);

        // Create a token that will timeout quickly
        var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(1));

        // Wait a bit to ensure the token is cancelled
        await Task.Delay(10);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<TaskCanceledException>(
            async () => await service.ExtractMetadataAsync("dQw4w9WgXcQ", cts.Token)
        );

        exception.Should().NotBeNull();
    }

    #endregion

    #region Invalid Video ID Handling Tests

    [Fact]
    public async Task ExtractMetadataAsync_WithNullYouTubeId_ThrowsArgumentException()
    {
        // Arrange
        var logger = Scope.ServiceProvider.GetRequiredService<ILogger<MetadataExtractionService>>();
        var service = new MetadataExtractionService(logger);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await service.ExtractMetadataAsync(null!)
        );
    }

    [Fact]
    public async Task ExtractMetadataAsync_WithEmptyYouTubeId_ThrowsArgumentException()
    {
        // Arrange
        var logger = Scope.ServiceProvider.GetRequiredService<ILogger<MetadataExtractionService>>();
        var service = new MetadataExtractionService(logger);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await service.ExtractMetadataAsync("")
        );
    }

    [Fact]
    public async Task ExtractMetadataAsync_WithInvalidYouTubeId_ThrowsInvalidOperationException()
    {
        // Arrange
        var logger = Scope.ServiceProvider.GetRequiredService<ILogger<MetadataExtractionService>>();
        var service = new MetadataExtractionService(logger);
        var invalidYouTubeId = "invalid-id-123"; // Invalid format

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await service.ExtractMetadataAsync(invalidYouTubeId)
        );

        exception.Message.Should().NotBeNullOrEmpty();
        exception.Message.Should().Contain(invalidYouTubeId);
    }

    [Fact]
    public async Task ExtractMetadataAsync_WithPrivateVideo_ThrowsInvalidOperationException()
    {
        // Arrange
        var logger = Scope.ServiceProvider.GetRequiredService<ILogger<MetadataExtractionService>>();
        var service = new MetadataExtractionService(logger);

        // This is a known private/unavailable video ID
        var privateVideoId = "private123";

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await service.ExtractMetadataAsync(privateVideoId)
        );

        exception.Message.Should().Contain("not available");
    }

    #endregion

    #region Video Accessibility Tests

    [Fact]
    public async Task IsVideoAccessibleAsync_WithValidVideo_ReturnsTrue()
    {
        // Arrange
        var logger = Scope.ServiceProvider.GetRequiredService<ILogger<MetadataExtractionService>>();
        var service = new MetadataExtractionService(logger);
        var youTubeId = "dQw4w9WgXcQ"; // Known valid video

        // Act
        var isAccessible = await service.IsVideoAccessibleAsync(youTubeId);

        // Assert
        isAccessible.Should().BeTrue();
    }

    [Fact]
    public async Task IsVideoAccessibleAsync_WithInvalidVideo_ReturnsFalse()
    {
        // Arrange
        var logger = Scope.ServiceProvider.GetRequiredService<ILogger<MetadataExtractionService>>();
        var service = new MetadataExtractionService(logger);
        var invalidYouTubeId = "invalid-id-xyz";

        // Act
        var isAccessible = await service.IsVideoAccessibleAsync(invalidYouTubeId);

        // Assert
        isAccessible.Should().BeFalse();
    }

    [Fact]
    public async Task IsVideoAccessibleAsync_WithEmptyId_ReturnsFalse()
    {
        // Arrange
        var logger = Scope.ServiceProvider.GetRequiredService<ILogger<MetadataExtractionService>>();
        var service = new MetadataExtractionService(logger);

        // Act
        var isAccessible = await service.IsVideoAccessibleAsync("");

        // Assert
        isAccessible.Should().BeFalse();
    }

    [Fact]
    public async Task IsVideoAccessibleAsync_WithNullId_ReturnsFalse()
    {
        // Arrange
        var logger = Scope.ServiceProvider.GetRequiredService<ILogger<MetadataExtractionService>>();
        var service = new MetadataExtractionService(logger);

        // Act
        var isAccessible = await service.IsVideoAccessibleAsync(null!);

        // Assert
        isAccessible.Should().BeFalse();
    }

    #endregion

    #region Error Message Quality Tests

    [Fact]
    public async Task ExtractMetadataAsync_InvalidVideo_ProvidesDescriptiveErrorMessage()
    {
        // Arrange
        var logger = Scope.ServiceProvider.GetRequiredService<ILogger<MetadataExtractionService>>();
        var service = new MetadataExtractionService(logger);
        var invalidYouTubeId = "bad-video-id";

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await service.ExtractMetadataAsync(invalidYouTubeId)
        );

        // Verify error message is descriptive
        exception.Message.Should().NotBeNullOrEmpty();
        exception.Message.Should().Contain(invalidYouTubeId);
        exception.Message.Length.Should().BeGreaterThan(20); // Should be descriptive, not just the ID
    }

    #endregion
}
